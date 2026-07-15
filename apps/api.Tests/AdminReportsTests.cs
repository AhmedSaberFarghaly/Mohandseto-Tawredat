using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.AdminReports;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminReportsTests : IDisposable
{
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private sealed class FakeDelivery : IReportDeliverySender
    {
        public int Calls { get; private set; }
        public Task SendAsync(string reportName, IReadOnlyList<string> recipients, IReadOnlyList<string> formats, int rows, CancellationToken ct)
        { Calls++; return Task.CompletedTask; }
    }

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly FakeDelivery _delivery = new();
    private readonly AdminReportService _service;
    private readonly Guid _actorId = Guid.NewGuid();

    public AdminReportsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated();
        _service = new AdminReportService(_db, _delivery);
        SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        _db.Users.Add(new User { Id = _actorId, FullName = "مدير التقارير", Phone = "01011112222", Email = "reports@test.local", IsPlatformStaff = true, IsActive = true });
        _db.Companies.AddRange(
            new Company { Tenant = new Tenant { Name = "شركة ألف", Status = TenantStatus.Active }, LegalName = "شركة ألف", Phone = "01000000001", CreditLimit = 500_000, Sector = "المقاولات", CustomerStage = CustomerStage.Active },
            new Company { Tenant = new Tenant { Name = "شركة باء", Status = TenantStatus.Active }, LegalName = "شركة باء", Phone = "01000000002", CreditLimit = 300_000, Sector = "التجارة", CustomerStage = CustomerStage.Qualified });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Dashboard_exposes_complete_catalog_and_real_options()
    {
        var dashboard = await _service.DashboardAsync();
        Assert.Equal(22, dashboard.Categories.Sum(x => x.Count));
        Assert.Equal(8, dashboard.Sources.Count);
        Assert.Equal(2, dashboard.Companies.Count);
        Assert.Contains(dashboard.Categories.SelectMany(x => x.Reports), x => x.Code == "profit");
    }

    [Fact]
    public async Task Built_in_and_custom_reports_use_database_rows_and_filters()
    {
        var builtIn = await _service.BuiltInAsync("companies", new(Search: "ألف"));
        Assert.Equal(1, builtIn.TotalRows);
        Assert.Equal(500_000m, builtIn.Kpis[0].Value);

        var custom = await _service.PreviewAsync(new("companies", ["company", "sector", "value"], new(), "sector", "Bar"));
        Assert.Equal(2, custom.TotalRows);
        Assert.Contains(custom.Columns, x => x.Code == "count");
    }

    [Fact]
    public async Task Every_built_in_report_executes_against_the_real_schema()
    {
        string[] codes = ["sales", "orders", "rfq", "conversion", "companies", "products", "categories", "inventory",
            "out-of-stock", "purchasing", "suppliers", "profit", "tax", "payments", "debts", "contracts",
            "printed-products", "delivery", "returns", "customer-service", "staff-performance", "sales-reps"];
        foreach (var code in codes)
        {
            var result = await _service.BuiltInAsync(code, new());
            Assert.Equal(code, result.Code);
            Assert.Equal(4, result.Kpis.Count);
        }
    }

    [Fact]
    public async Task Saved_template_can_be_scheduled_and_processed()
    {
        var saved = await _service.SaveAsync(_actorId, new(null, "تقرير الشركات اليومي", "companies", ["company", "value", "status"], new(), null, "Line", true));
        await _service.ScheduleAsync(_actorId, saved.Id, new("Daily", null, "09:00", ["Excel", "Pdf"], ["manager@example.com"], true));
        var entity = await _db.SavedReports.SingleAsync(x => x.Id == saved.Id);
        entity.NextRunAt = DateTime.UtcNow.AddMinutes(-1);
        await _db.SaveChangesAsync();

        Assert.Equal(1, await _service.ProcessDueAsync());
        Assert.Equal(1, _delivery.Calls);
        var run = await _db.ReportRuns.SingleAsync();
        Assert.Equal(ReportRunStatus.Completed, run.Status);
        Assert.Equal(2, run.RowCount);
        Assert.NotNull(entity.LastRunAt);
        Assert.True(entity.NextRunAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Excel_and_pdf_exports_are_valid_file_containers()
    {
        var options = new ReportExportOptionsDto("companies", new());
        var excel = await _service.ExportExcelAsync(options);
        var pdf = await _service.ExportPdfAsync(options);
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(excel, 0, 2));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.True(excel.Length > 500);
        Assert.True(pdf.Length > 300);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
