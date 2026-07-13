using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminQuotes;
using Mohandseto.Api.Application.Rfq;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminQuoteManagementTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminQuoteService _service;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-admin-quotes", Guid.NewGuid().ToString("N"));
    private readonly Guid _rfqId = Guid.NewGuid();
    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private Guid _branchId;
    private Guid _centerId;
    private List<Product> _products = [];

    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public AdminQuoteManagementTests()
    {
        Directory.CreateDirectory(_root); _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        var tenant = new PlatformTenant(); _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, tenant);
        _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        var rfqs = new RfqService(_db, tenant, new TestEnvironment(_root)); _service = new AdminQuoteService(_db, rfqs);
        SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        var tenant = new Tenant { Name = "شركة عروض الأسعار", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenant.Id, Tenant = tenant, LegalName = "شركة عروض الأسعار", Phone = "01090000001" };
        var customer = new User { Id = _userId, TenantId = tenant.Id, FullName = "مسؤول المشتريات", Phone = "01090000002", Email = "quotes@test.com" };
        var staff = new User { Id = _staffId, FullName = "مسؤول عروض الأسعار", Phone = "01090000003", IsPlatformStaff = true };
        var branch = new CompanyBranch { TenantId = tenant.Id, CompanyId = company.Id, Name = "المقر الرئيسي", Governorate = "القاهرة", AddressLine = "مدينة نصر" };
        var center = new CostCenter { TenantId = tenant.Id, Code = "CC-QT", NameAr = "مشتريات التشغيل", BudgetAmount = 100000, ApprovalThreshold = 50000 };
        _branchId = branch.Id; _centerId = center.Id; _products = await _db.Products.Where(x => x.Status == ProductStatus.Active).Take(2).ToListAsync();
        var rfq = new Rfq { Id = _rfqId, TenantId = tenant.Id, UserId = customer.Id, Number = "RFQ-ADMIN-1", Title = "احتياجات تشغيلية",
            RequiredDate = DateTime.UtcNow.AddDays(20), QuoteDeadline = DateTime.UtcNow.AddDays(5), Status = RfqStatus.NeedsReview, DeliveryGovernorate = "القاهرة" };
        rfq.Items.Add(new RfqItem { TenantId = tenant.Id, DescriptionAr = "صنف مستخرج", Quantity = 10, UnitName = "قطعة", Source = RfqItemSource.Pdf, ExtractionConfidence = .72m });
        rfq.Items.Add(new RfqItem { TenantId = tenant.Id, ProductId = _products[1].Id, DescriptionAr = _products[1].NameAr, SkuHint = _products[1].Sku,
            Quantity = 4, UnitName = "قطعة", Source = RfqItemSource.Catalog, ExtractionConfidence = 1, IsReviewed = true });
        _db.AddRange(tenant, company, customer, staff, branch, center, rfq); await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Admin_quote_cycle_covers_extraction_suppliers_margin_versions_negotiation_acceptance_and_conversion()
    {
        var page = await _service.ListAsync("RFQ-ADMIN", "NeedsReview"); Assert.Single(page.Items); Assert.Equal(1, page.Summary.NeedsReview);
        var detail = await _service.DetailAsync(_rfqId); var extracted = detail.Items.Single(x => !x.IsReviewed);
        detail = await _service.ReviewItemAsync(_rfqId, extracted.Id, new("صنف تمت مراجعته", 10, "قطعة", "جودة عالية", null, true));
        detail = await _service.CreateTemporaryAsync(_rfqId, extracted.Id, new("منتج مؤقت", "حسب العينة", 60, 7));
        Assert.True(detail.Items.Single(x => x.Id == extracted.Id).IsTemporary);
        detail = await _service.LinkProductAsync(_rfqId, extracted.Id, _products[0].Id); Assert.False(detail.Items.Single(x => x.Id == extracted.Id).IsTemporary);

        detail = await _service.RequestSupplierAsync(_rfqId, new(null, "المورد الأول", "أحمد", "0109", "supplier@test.com", DateTime.UtcNow.AddDays(2)));
        var supplier = Assert.Single(detail.Suppliers);
        detail = await _service.RecordSupplierPriceAsync(_rfqId, new(supplier.Id, "SUP-Q-1", DateTime.UtcNow.AddDays(10),
            detail.Items.Select((x, index) => new SupplierPriceLineDto(x.Id, 50 + index * 10, null)).ToList()));
        Assert.Single(detail.SupplierQuotes); Assert.Equal("Received", detail.SupplierRequests.Single().Status);

        var input = detail.Items.Select((x, index) => new SaveCustomerQuoteLineDto(x.Id, x.ProductId, null, 50 + index * 10, 75 + index * 15, 6)).ToList();
        detail = await _service.SaveQuoteAsync(_rfqId, new(input, 100, "Percent", 5, 14, 6, "تحويل بنكي خلال 30 يومًا", "الإصدار الأول"));
        var version = Assert.Single(detail.Versions); Assert.Equal("Draft", detail.QuoteStatus); Assert.True(version.DiscountAmount > 0);
        detail = await _service.SendAsync(_rfqId, version.Id); Assert.Equal("Quoted", detail.Status);
        detail = await _service.NegotiateAsync(_staffId, _rfqId, new("تم اعتماد خصم الكمية", version.Total - 100, "CounterOffer")); Assert.Single(detail.Negotiations);
        detail = await _service.AcceptAsync(_rfqId, version.Id); Assert.Equal("Accepted", detail.Status);

        var template = await _service.SaveTemplateAsync(null, new("شركات", "القالب القياسي", 14, 6, "تحويل بنكي", "Percent", 5));
        Assert.Equal("شركات", template.Name); Assert.Single(await _service.TemplatesAsync());
        var options = await _service.ConversionOptionsAsync(_rfqId); Assert.Single(options.Branches); Assert.Single(options.CostCenters);
        var order = await _service.ConvertAsync(_rfqId, new(_branchId, _centerId, "أمين المخزن", "01090000004"));
        Assert.Equal("Confirmed", order.Status); Assert.Equal(RfqStatus.Converted, (await _db.Rfqs.SingleAsync(x => x.Id == _rfqId)).Status);
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
