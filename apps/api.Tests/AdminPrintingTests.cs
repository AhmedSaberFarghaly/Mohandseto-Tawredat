using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.AdminPrinting;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminPrintingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TenantProvider _tenant = new();
    private readonly CustomizationService _customization;
    private readonly AdminPrintingService _printing;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-printing-tests", Guid.NewGuid().ToString("N"));
    private Guid _customer;
    private Guid _designer;

    private sealed class TenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class EnvironmentStub(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Mohandseto.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public AdminPrintingTests()
    {
        Directory.CreateDirectory(_root); _connection = new("DataSource=:memory:"); _connection.Open();
        _db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant); _db.Database.EnsureCreated();
        CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        CustomizationSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        SeedAsync().GetAwaiter().GetResult();
        _customization = new(_db, _tenant, new EnvironmentStub(_root)); _printing = new(_db);
    }

    private async Task SeedAsync()
    {
        var tenant = new Tenant { Name = "شركة طباعة الاختبار", Status = TenantStatus.Active };
        var company = new Company { Tenant = tenant, TenantId = tenant.Id, LegalName = tenant.Name, Phone = "0228000000" };
        _customer = Guid.NewGuid(); _designer = Guid.NewGuid();
        var role = new Role { Code = "graphic_designer", NameAr = "مصمم جرافيك", NameEn = "Graphic Designer", IsSystem = true };
        var designer = new User { Id = _designer, FullName = "مريم المصممة", Phone = "01098000001", IsActive = true, IsPlatformStaff = true };
        designer.Roles.Add(new UserRole { User = designer, Role = role, RoleId = role.Id });
        _db.AddRange(company, new User { Id = _customer, TenantId = tenant.Id, FullName = "مسؤول العميل", Phone = "01098000002", IsActive = true }, designer);
        await _db.SaveChangesAsync(); _tenant.TenantId = tenant.Id;
    }

    private async Task<CustomRequestDto> DesignRequest()
    {
        var template = await _customization.TemplateAsync((await _customization.TemplatesAsync()).First().Id);
        var created = await _customization.CreateAsync(_customer, new CreateCustomRequestForm
        {
            TemplateId = template.Id, Quantity = 100, PlacementId = template.Placements.First().Id,
            PrintMethodId = template.PrintMethods.First().Id, MaterialId = template.Materials.First().Id,
            ColorId = template.Colors.First().Id, SizeId = template.Sizes.First().Id,
            DesignServiceRequested = true, Objective = "هدايا مؤتمر الشركة", DesiredDate = DateTime.UtcNow.AddDays(10),
            Logo = Upload("company-logo.ai", "application/illustrator")
        });
        await _customization.SetQuoteAsync(created.Id, new(created.EstimatedTotal, DateTime.UtcNow.AddDays(5), 7));
        return await _customization.RespondToQuoteAsync(_customer, created.Id, true);
    }

    [Fact]
    public async Task Printing_admin_runs_design_logo_sample_production_quality_packaging_and_ready_cycle()
    {
        var request = await DesignRequest();
        await _printing.AssignDesignerAsync(_designer, request.Id, new(_designer, DateTime.UtcNow.AddDays(3), "أولوية عالية"));
        await _printing.ReviewLogoAsync(_designer, request.Id, request.Assets.Single().Id, new(true, 95, true, true, true, true, true, "جاهز للطباعة"));
        var version = await _customization.PublishDesignAsync(request.Id, _designer, new PublishDesignForm { Title = "التصميم الأول", File = Upload("design-v1.png", "image/png") });
        await _printing.SendDesignAsync(_designer, request.Id, new(version.Versions.Single().Id, "برجاء مراجعة النسخة"));
        var approved = await _customization.DesignDecisionAsync(_customer, request.Id, new(version.Versions.Single().Id, "Approved", "معتمد"));
        var cart = new CartService(_db, _tenant); await cart.AddCustomRequestAsync(_customer, approved.Id);
        await _customization.StartForOrderAsync([request.Id], Guid.NewGuid(), false); await _db.SaveChangesAsync();
        var sample = await _customization.PublishSampleAsync(request.Id, new PublishSampleForm { Note = "عينة فعلية", File = Upload("sample.png", "image/png") });
        await _customization.SampleDecisionAsync(_customer, request.Id, sample.Production!.Samples.Single().Id, new("Approved", "مطابقة"));
        await _printing.StartProductionAsync(_designer, request.Id, new(DateTime.UtcNow, DateTime.UtcNow.AddDays(4)));
        await _printing.SavePackagingAsync(_designer, request.Id, new("علب كرتون محكمة", 20, 5));

        var current = await _printing.DetailAsync(request.Id);
        foreach (var stage in current.Production!.Stages)
        {
            if (stage.Code == "quality") await _customization.AddQualityCheckAsync(request.Id, _designer, new("مطابقة الألوان والتشطيب", true, "ناجح"));
            await _printing.UpdateStageAsync(_designer, request.Id, stage.Id, new("Completed", 100, null));
        }
        await _printing.MarkReadyAsync(_designer, request.Id, new("SHP-PENDING"));

        var detail = await _printing.DetailAsync(request.Id); var dashboard = await _printing.DashboardAsync();
        Assert.Equal("Ready", detail.Request.Status); Assert.Equal(100, detail.Production!.ProducedQuantity);
        Assert.Equal(5, detail.Production.PackageCount); Assert.Equal("Approved", detail.Assets.Single().QualityStatus);
        Assert.NotNull(detail.Versions.Single().SentToCustomerAt); Assert.Contains(detail.Comments, x => x.IsInternal);
        Assert.Contains(detail.Comments, x => !x.IsInternal); Assert.Single(detail.Approvals);
        Assert.Single(dashboard.Requests); Assert.Single(dashboard.Designers); Assert.Equal(30, dashboard.Templates.Count);
        Assert.Equal(2, await _db.Notifications.CountAsync());
        Assert.Contains(await _db.AuditLogs.ToListAsync(), x => x.Action == "printing.ready");

        var template = dashboard.Templates.First();
        await _printing.UpdateTemplateAsync(_designer, template.Id, new(template.Name + " محدث", template.Description, template.SetupFee + 10, template.MinQuantity, template.LeadTimeDays, true));
        Assert.EndsWith("محدث", (await _db.CustomProductTemplates.FindAsync(template.Id))!.NameAr);
    }

    [Fact]
    public async Task Printing_guards_require_logo_review_latest_design_sample_sequence_quality_and_completion()
    {
        var request = await DesignRequest();
        var first = await _customization.PublishDesignAsync(request.Id, _designer, new PublishDesignForm { Title = "v1", File = Upload("v1.png", "image/png") });
        await Assert.ThrowsAsync<ApiException>(() => _printing.SendDesignAsync(_designer, request.Id, new(first.Versions.Single().Id, null)));
        await Assert.ThrowsAsync<ApiException>(() => _printing.ReviewLogoAsync(_designer, request.Id, request.Assets.Single().Id, new(true, 40, false, false, false, false, false, null)));
        await _printing.ReviewLogoAsync(_designer, request.Id, request.Assets.Single().Id, new(true, 90, true, true, true, true, true, null));
        await _printing.SendDesignAsync(_designer, request.Id, new(first.Versions.Single().Id, null));
        await _customization.DesignDecisionAsync(_customer, request.Id, new(first.Versions.Single().Id, "Approved", null));
        var cart = new CartService(_db, _tenant); await cart.AddCustomRequestAsync(_customer, request.Id);
        await _customization.StartForOrderAsync([request.Id], Guid.NewGuid(), false); await _db.SaveChangesAsync();
        await Assert.ThrowsAsync<ApiException>(() => _printing.StartProductionAsync(_designer, request.Id, new(null, null)));
        var sample = await _customization.PublishSampleAsync(request.Id, new PublishSampleForm { File = Upload("sample.png", "image/png") });
        await _customization.SampleDecisionAsync(_customer, request.Id, sample.Production!.Samples.Single().Id, new("Approved", null));
        await _printing.StartProductionAsync(_designer, request.Id, new(null, null));
        var detail = await _printing.DetailAsync(request.Id);
        await Assert.ThrowsAsync<ApiException>(() => _printing.UpdateStageAsync(_designer, request.Id, detail.Production!.Stages[1].Id, new("Completed", 100, null)));
        await Assert.ThrowsAsync<ApiException>(() => _printing.MarkReadyAsync(_designer, request.Id, new(null)));
        await Assert.ThrowsAsync<ApiException>(() => _printing.UpdateTemplateAsync(_designer, detail.Request.Id, new("", null, -1, 0, 0, true)));
    }

    private static IFormFile Upload(string name, string type)
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = type };
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
