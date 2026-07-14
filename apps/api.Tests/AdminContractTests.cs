using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminContracts;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminContractTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminContractService _service;
    private readonly Guid _actor = Guid.NewGuid();
    private Company _company = null!;
    private User _customer = null!;
    private List<Product> _products = [];
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }

    public AdminContractTests()
    {
        _connection = new("DataSource=:memory:"); _connection.Open();
        _db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant()); _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult(); SeedAsync().GetAwaiter().GetResult(); _service = new(_db);
    }

    private async Task SeedAsync()
    {
        var tenant = new Tenant { Name = "شركة عقود الاختبار", Status = TenantStatus.Active }; _company = new Company { Tenant = tenant, TenantId = tenant.Id, LegalName = tenant.Name, Phone = "0227000000", CreditLimit = 50_000 }; _customer = new User { TenantId = tenant.Id, FullName = "مسؤول الشركة", Phone = "01097000001", Email = "contracts@test.local", IsActive = true }; _db.AddRange(_company, _customer, new User { Id = _actor, FullName = "مدير العقود", Phone = "01097000002", IsPlatformStaff = true }); await _db.SaveChangesAsync(); _products = await _db.Products.Take(2).ToListAsync();
    }

    private SaveContractDto Input() => new(_company.Id, "SpecialPrices", DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddYears(1), true, true, 30, "FixedPerProduct", 0, 0, 30, 100_000, 2, 10, 1, "BankTransfer", "High", 24, 2, true, 6, "عقد أسعار خاصة سنوي", _products.Select((x, i) => new SaveContractProductDto(x.Id, Math.Max(1, x.BasePrice - 10 - i), 100 + i * 50)).ToList(), [new(1, 499, 0), new(500, null, 3)]);

    [Fact]
    public async Task Contract_cycle_covers_creation_approvals_attachment_activation_prices_renewal_revision_and_health()
    {
        var id = await _service.CreateAsync(_actor, Input()); var draft = await _service.DetailAsync(id); Assert.Equal("PendingApproval", draft.Contract.Status); Assert.Equal(2, draft.Approvals.Count); Assert.Equal(2, draft.Products.Count); Assert.True(draft.Products.All(x => x.MarginPercent != 0));
        await _service.AddAttachmentAsync(_actor, id, new("SignedContract", "signed-contract.pdf", "/contracts/signed-contract.pdf", "application/pdf", 2048));
        foreach (var approval in draft.Approvals.OrderBy(x => x.Sequence)) await _service.DecideApprovalAsync(_actor, id, approval.Id, new("Approved", "معتمد"));
        await _service.ActivateAsync(_actor, id, new(true, true));
        var active = await _service.DetailAsync(id); Assert.Equal("Active", active.Contract.Status); Assert.NotNull(active.ActivatedAt); Assert.NotNull(active.CustomerNotifiedAt); Assert.Equal(2, await _db.CompanyProductPrices.CountAsync()); Assert.Single(await _db.Notifications.ToListAsync()); Assert.Equal(100_000, (await _db.Companies.SingleAsync()).CreditLimit);

        var oldEnd = active.Contract.EndsAt; var oldPrice = active.Products[0].ContractPrice;
        await _service.RenewAsync(_actor, id, new(12, 3, 120_000, "تجديد سنوي")); var renewed = await _service.DetailAsync(id); Assert.Equal(oldEnd.AddMonths(12), renewed.Contract.EndsAt); Assert.True(renewed.Products[0].ContractPrice > oldPrice); Assert.Single(await _db.ContractRenewalRequests.ToListAsync());
        await _service.CreatePriceRevisionAsync(_actor, id, new(DateTime.UtcNow.AddMinutes(-1), true, "تحديث التكلفة", [new(_products[0].Id, renewed.Products[0].ContractPrice + 5, "زيادة مورد")])); var revised = await _service.DetailAsync(id); Assert.Equal("Applied", revised.PriceRevisions.Single().Status); Assert.Equal(renewed.Products[0].ContractPrice + 5, revised.Products.Single(x => x.ProductId == _products[0].Id).ContractPrice);
        var scheduledPrice = revised.Products.Single(x => x.ProductId == _products[1].Id).ContractPrice + 7;
        await _service.CreatePriceRevisionAsync(_actor, id, new(DateTime.UtcNow.AddHours(1), true, "مراجعة مجدولة", [new(_products[1].Id, scheduledPrice, null)]));
        var scheduled = await _db.ContractPriceRevisions.SingleAsync(x => x.Status == ContractPriceRevisionStatus.Scheduled); scheduled.EffectiveAt = DateTime.UtcNow.AddMinutes(-1); await _db.SaveChangesAsync();
        Assert.Equal(1, await _service.ProcessDuePriceRevisionsAsync()); Assert.Equal(ContractPriceRevisionStatus.Applied, scheduled.Status); Assert.Equal(scheduledPrice, (await _db.CompanyContractProducts.SingleAsync(x => x.ProductId == _products[1].Id)).ContractPrice);
        var dashboard = await _service.DashboardAsync(); Assert.Equal(1, dashboard.Kpis.ActiveContracts); Assert.Single(dashboard.Contracts); Assert.Equal(250, dashboard.Products.Count); Assert.True(revised.Health.AverageMarginPercent != 0);
    }

    [Fact]
    public async Task Contract_guards_enforce_approval_order_signed_document_and_non_overlapping_tiers()
    {
        var id = await _service.CreateAsync(_actor, Input()); var detail = await _service.DetailAsync(id); var approvals = detail.Approvals.OrderBy(x => x.Sequence).ToList();
        await Assert.ThrowsAsync<ApiException>(() => _service.DecideApprovalAsync(_actor, id, approvals[1].Id, new("Approved", null)));
        await _service.DecideApprovalAsync(_actor, id, approvals[0].Id, new("Approved", null)); await _service.DecideApprovalAsync(_actor, id, approvals[1].Id, new("Approved", null)); await Assert.ThrowsAsync<ApiException>(() => _service.ActivateAsync(_actor, id, new(true, false)));
        var invalid = Input() with { CompanyId = _company.Id, QuantityTiers = [new(1, 500, 0), new(400, null, 3)] }; await Assert.ThrowsAsync<ApiException>(() => _service.CreateAsync(_actor, invalid));
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
