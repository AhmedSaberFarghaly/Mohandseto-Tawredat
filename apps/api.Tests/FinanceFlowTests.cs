using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class FinanceFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-finance-tests", Guid.NewGuid().ToString("N"));
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
    public FinanceFlowTests()
    {
        Directory.CreateDirectory(_root); _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant); _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Invoice_runs_through_tax_pdf_excel_bank_transfer_and_credit_limit()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Finance Co", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "Finance Co", Phone = "01060000000", CreditLimit = 50000 };
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Finance Buyer", Phone = "01060000111", PhoneVerified = true };
        var order = new Order { TenantId = tenantId, UserId = userId, Number = $"ORD-FIN-{Guid.NewGuid():N}", BranchId = Guid.NewGuid(), BranchName = "Main", DeliveryAddress = "Cairo", ReceiverName = "Buyer", ReceiverPhone = "0106", RequiredDate = DateTime.UtcNow.AddDays(2), ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.BankTransfer, Status = OrderStatus.Confirmed, Subtotal = 1000, TaxIncluded = 140, Shipping = 20, Total = 1160 };
        order.Items.Add(new OrderItem { TenantId = tenantId, ProductId = Guid.NewGuid(), Sku = "FIN-SKU", NameAr = "Finance Item", Quantity = 10, UnitPrice = 100, LineTotal = 1000 });
        _db.AddRange(tenant, company, user, order); await _db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Company:TaxNumber"] = "TAX-123", ["Payments:BankIban"] = "EG-IBAN-TEST" }).Build();
        var service = new FinanceService(_db, _tenant, new TestEnvironment(_root), config);
        var invoice = service.IssueForOrder(order); await _db.SaveChangesAsync();

        var list = await service.ListAsync("Issued", "FIN", null, null); Assert.Single(list); Assert.Equal(1160, list[0].Outstanding);
        var detail = await service.DetailAsync(invoice.Id); Assert.Equal("TAX-123", detail.SellerTaxNumber); Assert.Single(detail.Lines); Assert.Contains("EGS|", detail.ElectronicQrPayload);
        var pdf = await service.PdfAsync(invoice.Id); Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf));
        var xlsx = await service.ExportFileAsync("Issued", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), "xlsx");
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(xlsx.Content, 0, 2)); Assert.EndsWith(".xlsx", xlsx.FileName);
        var csv = await service.ExportFileAsync("Issued", null, null, "csv");
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, csv.Content[..3]); Assert.Contains(invoice.Number, System.Text.Encoding.UTF8.GetString(csv.Content));
        var exportedPdf = await service.ExportFileAsync("Issued", null, null, "pdf");
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(exportedPdf.Content));
        await Assert.ThrowsAsync<Mohandseto.Api.Application.Common.ApiException>(() => service.ExportFileAsync(null, null, null, "docx"));
        var summary = await service.SummaryAsync(); Assert.Equal(50000, summary.CreditLimit); Assert.Equal(1160, summary.Outstanding);

        var started = await service.StartPaymentAsync(userId, invoice.Id, new StartInvoicePaymentDto(1160, "BANK-1")); Assert.Equal("EG-IBAN-TEST", started.Iban);
        var receiptBytes = "%PDF-1.4"u8.ToArray(); var receipt = new FormFile(new MemoryStream(receiptBytes), 0, receiptBytes.Length, "file", "receipt.pdf") { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
        var payment = await service.UploadReceiptAsync(userId, started.PaymentId, receipt); Assert.Equal("PendingVerification", payment.Status);
        payment = await service.DecidePaymentAsync(Guid.NewGuid(), started.PaymentId, new PaymentDecisionDto(true, null)); Assert.Equal("Completed", payment.Status);
        detail = await service.DetailAsync(invoice.Id); Assert.Equal("Paid", detail.Status); Assert.Equal(0, detail.Outstanding);

        var credit = await service.RequestCreditAsync(userId, new CreditLimitRequestDto(100000, "نمو أعمال الشركة")); Assert.Equal("Submitted", credit.Status);
        credit = await service.DecideCreditAsync(Guid.NewGuid(), credit.Id, new CreditDecisionDto(true, 90000, "موافقة جزئية")); Assert.Equal("Approved", credit.Status);
        Assert.Equal(90000, (await _db.Companies.SingleAsync()).CreditLimit);

        _tenant.TenantId = Guid.NewGuid(); Assert.Empty(await service.ListAsync(null, null, null, null));
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
