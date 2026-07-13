using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Rfq;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class RfqFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-rfq-tests", Guid.NewGuid().ToString("N"));
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }
    public RfqFlowTests()
    {
        Directory.CreateDirectory(_root); _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated(); CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Rfq_runs_from_catalog_and_file_extraction_through_versions_negotiation_and_order()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "RFQ Co", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "RFQ Co", Phone = "01000000000" };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "مشتري RFQ", Phone = "01010000111", PhoneVerified = true };
        var branch = new CompanyBranch { TenantId = tenantId, CompanyId = company.Id, Name = "المقر", Governorate = "القاهرة", City = "المعادي", AddressLine = "شارع 9" };
        var center = new CostCenter { TenantId = tenantId, Code = "CC-RFQ", NameAr = "ميزانية RFQ", BudgetAmount = 100000, ApprovalThreshold = 1 };
        _db.AddRange(tenant, company, user, branch, center); await _db.SaveChangesAsync();
        var products = await _db.Products.Where(p => p.Status == ProductStatus.Active).Take(2).ToListAsync();
        var service = new RfqService(_db, _tenant, new TestEnvironment(_root));
        var rfq = await service.CreateAsync(userId, new CreateRfqDto("احتياجات تشغيل يوليو", "توريد عاجل",
            DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(5), "القاهرة"));
        rfq = await service.AddItemAsync(userId, rfq.Id, new UpsertRfqItemDto(products[0].Id, products[0].NameAr, 10, "قطعة", null, null, true, "Catalog"));

        var csv = "description,quantity,unit\nعبوات إضافية,20,عبوة\n"u8.ToArray();
        var upload = new FormFile(new MemoryStream(csv), 0, csv.Length, "file", "requirements.csv")
        { Headers = new HeaderDictionary(), ContentType = "text/csv" };
        var attachment = await service.UploadAsync(userId, rfq.Id, upload);
        Assert.Equal("Completed", attachment.ExtractionStatus);
        rfq = await service.DetailAsync(userId, rfq.Id);
        var extracted = rfq.Items.Single(i => i.Source == "Excel"); Assert.False(extracted.IsReviewed);
        rfq = await service.UpdateItemAsync(userId, rfq.Id, extracted.Id,
            new UpsertRfqItemDto(products[1].Id, extracted.Description, extracted.Quantity, extracted.UnitName, "جودة ممتازة", null, true, "Excel"));
        rfq = await service.SubmitAsync(userId, rfq.Id); Assert.Equal("UnderReview", rfq.Status);

        var firstItems = rfq.Items.Select((item, index) => new PublishQuoteItemDto(item.Id, item.ProductId, 100 + index * 25)).ToList();
        rfq = await service.PublishQuoteAsync(rfq.Id, new PublishQuoteDto(firstItems, 100, 7, 4, "السداد خلال 30 يومًا", "العرض الأول"), Guid.NewGuid());
        Assert.Equal("Quoted", rfq.Status); Assert.Single(rfq.QuoteVersions);
        var first = rfq.QuoteVersions.Single();
        rfq = await service.NegotiateAsync(userId, rfq.Id, new NegotiationRequestDto("نحتاج خصمًا للكميات", first.Total - 200, "CounterOffer"));
        Assert.Equal("Negotiating", rfq.Status); Assert.Single(rfq.Negotiations);
        var secondItems = rfq.Items.Select((item, index) => new PublishQuoteItemDto(item.Id, item.ProductId, 90 + index * 20)).ToList();
        rfq = await service.PublishQuoteAsync(rfq.Id, new PublishQuoteDto(secondItems, 0, 7, 5, "السداد خلال 30 يومًا", "خصم تفاوضي"), Guid.NewGuid());
        Assert.Equal(2, rfq.QuoteVersions.Count); Assert.True(rfq.QuoteVersions[0].Total < first.Total);
        var accepted = rfq.QuoteVersions.First();
        rfq = await service.QuoteDecisionAsync(userId, rfq.Id, "accept", new QuoteDecisionDto(accepted.Id, "موافق"));
        Assert.Equal("Accepted", rfq.Status);
        var pdf = await service.QuotePdfAsync(userId, rfq.Id, accepted.Id); Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf));

        var order = await service.ConvertAsync(userId, rfq.Id, new ConvertRfqDto(branch.Id, center.Id, "أمين المخزن", "01010000222"));
        Assert.True(order.RequiresApproval); Assert.Equal(accepted.Total, order.Total);
        Assert.Equal(rfq.Id, (await _db.Orders.SingleAsync(o => o.Id == order.OrderId)).SourceRfqId);
        Assert.Equal(RfqStatus.Converted, (await _db.Rfqs.SingleAsync(r => r.Id == rfq.Id)).Status);

        _tenant.TenantId = Guid.NewGuid();
        await Assert.ThrowsAsync<ApiException>(() => service.DetailAsync(userId, rfq.Id));
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
