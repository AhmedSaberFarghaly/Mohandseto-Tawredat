using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminCrm;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminCrmTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminCrmService _service;
    private readonly Guid _actor = Guid.NewGuid();
    private readonly Guid _sales = Guid.NewGuid();
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }

    public AdminCrmTests()
    {
        _connection = new("DataSource=:memory:"); _connection.Open();
        _db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        _db.Users.AddRange(new User { Id = _actor, FullName = "مدير النظام", Phone = "01099000001", IsPlatformStaff = true }, new User { Id = _sales, FullName = "أحمد المبيعات", Phone = "01099000002", IsPlatformStaff = true, JobTitle = "مدير حساب" });
        _db.SaveChanges(); _service = new(_db);
    }

    private SaveCrmCompanyDto CompanyInput() => new("شركة النيل للتوريدات", "Nile Supplies", "0223456789", "info@nile.test", "CR-100", "TAX-100", "القاهرة", "مدينة نصر", "شارع الطيران", "التوريدات", "الشركات", "مستلزمات مكتبية", "KeyAccount", "Medium", _sales, "إحالة عميل", 150000, "محمود علي", "01099000003", "mahmoud@nile.test");

    [Fact]
    public async Task Crm_profile_cycle_covers_company_stage_activity_task_branch_credit_suspend_and_reactivate()
    {
        var id = await _service.CreateCompanyAsync(_actor, CompanyInput());
        await _service.ChangeStageAsync(_actor, id, new("Qualified", "تم تأهيل الاحتياج"));
        await _service.AddActivityAsync(_actor, id, new("Call", "مكالمة التعارف", "مناقشة احتياجات الربع القادم", DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null));
        await _service.AddActivityAsync(_actor, id, new("Meeting", "اجتماع العرض", "عرض قائمة الأسعار", DateTime.UtcNow.AddHours(1), null, null));
        await _service.AddTaskAsync(_actor, id, new("إرسال العرض", "متابعة قرار المشتريات", _sales, DateTime.UtcNow.AddDays(1), "High"));
        await _service.AddBranchAsync(_actor, id, new("فرع أكتوبر", "الجيزة", "6 أكتوبر", "المحور المركزي", "0230000000", false));
        await _service.UpdateCreditAsync(_actor, id, new(200000));
        await _service.ChangeStatusAsync(_actor, id, new("Suspended", "مراجعة المستندات"));
        await _service.ChangeStatusAsync(_actor, id, new("Active", "اكتملت المراجعة"));

        var detail = await _service.DetailAsync(id);
        Assert.Equal("Qualified", detail.Company.Stage); Assert.Equal("Active", detail.Company.Status); Assert.Equal(200000, detail.Company.CreditLimit);
        Assert.Equal(2, detail.Branches.Count); Assert.Equal(2, detail.Activities.Count); var task = Assert.Single(detail.Tasks); Assert.Equal("High", task.Priority);
        await _service.CompleteTaskAsync(_actor, task.Id); Assert.Equal("Completed", (await _service.DetailAsync(id)).Tasks.Single().Status);
        Assert.Single(await _db.CustomerStageHistories.ToListAsync());
        Assert.Contains(await _db.AuditLogs.ToListAsync(), x => x.Action == "crm.company.suspend");
    }

    [Fact]
    public async Task Crm_detail_aggregates_quotes_orders_products_contract_prices_statement_and_support()
    {
        var id = await _service.CreateCompanyAsync(_actor, CompanyInput() with { LegalName = "شركة الدلتا", Phone = "0222222222", PrimaryContactPhone = "01099000004" });
        var company = await _db.Companies.SingleAsync(x => x.Id == id); var tenantId = company.TenantId; var product = await _db.Products.FirstAsync();
        var branch = await _db.CompanyBranches.FirstAsync(x => x.CompanyId == id); var customer = await _db.Users.FirstAsync(x => x.TenantId == tenantId);
        var order = new Order { TenantId = tenantId, Number = "ORD-CRM-1", UserId = customer.Id, BranchId = branch.Id, BranchName = branch.Name, DeliveryAddress = branch.AddressLine ?? "القاهرة", ReceiverName = customer.FullName, ReceiverPhone = customer.Phone, RequiredDate = DateTime.UtcNow.AddDays(3), ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.CreditLine, Status = OrderStatus.Completed, Subtotal = 2000, TaxIncluded = 280, Total = 2280, Items = [new OrderItem { TenantId = tenantId, ProductId = product.Id, Sku = product.Sku, NameAr = product.NameAr, Quantity = 20, UnitPrice = 100, LineTotal = 2000 }] };
        var rfq = new Rfq { TenantId = tenantId, Number = "RFQ-CRM-1", UserId = customer.Id, Title = "طلب توريد", RequiredDate = DateTime.UtcNow.AddDays(10), QuoteDeadline = DateTime.UtcNow.AddDays(2), Status = RfqStatus.Quoted };
        var quote = new CustomerQuote { TenantId = tenantId, Rfq = rfq, Number = "QUO-CRM-1", Status = CustomerQuoteStatus.Sent, CurrentVersion = 1, Versions = [new CustomerQuoteVersion { TenantId = tenantId, VersionNumber = 1, Subtotal = 3000, Tax = 420, Total = 3420, ValidUntil = DateTime.UtcNow.AddDays(14), DeliveryDays = 3, SentAt = DateTime.UtcNow }] };
        var invoice = new Invoice { TenantId = tenantId, Number = "INV-CRM-1", Order = order, UserId = customer.Id, Status = InvoiceStatus.PartiallyPaid, IssuedAt = DateTime.UtcNow.Date, DueAt = DateTime.UtcNow.AddDays(30), SellerTaxNumber = "SELLER", BuyerTaxNumber = "TAX-CRM", Subtotal = 2000, Tax = 280, Total = 2280, PaidAmount = 1000 };
        _db.AddRange(order, quote, invoice,
            new CompanyContract { TenantId = tenantId, CompanyId = id, Number = "CTR-CRM-1", StartsAt = DateTime.UtcNow.Date, EndsAt = DateTime.UtcNow.AddYears(1), Status = CompanyContractStatus.Active, CreditLimit = 200000 },
            new SupportTicket { TenantId = tenantId, Number = "SUP-CRM-1", UserId = customer.Id, Type = SupportTicketType.Order, Priority = SupportTicketPriority.High, Subject = "متابعة التسليم", Description = "اختبار", Status = SupportTicketStatus.Open });
        await _db.SaveChangesAsync();
        await _service.ReplacePricesAsync(_actor, id, new([new(product.Id, product.BasePrice * .9m, DateTime.UtcNow.Date, DateTime.UtcNow.AddMonths(6))]));

        var detail = await _service.DetailAsync(id);
        Assert.Equal(2280, detail.Company.TotalPurchases); Assert.Equal(2280, detail.AverageOrderValue); Assert.Single(detail.Orders); Assert.Single(detail.Quotes); Assert.Single(detail.TopProducts);
        Assert.Single(detail.Contracts); Assert.Single(detail.SpecialPrices); Assert.Single(detail.AccountStatement); Assert.Equal(1280, detail.AccountStatement.Single().Balance); Assert.Single(detail.SupportTickets);
        Assert.NotEmpty(detail.UpsellOpportunities);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
