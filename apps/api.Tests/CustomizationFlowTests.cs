using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Infrastructure;
using Mohandseto.Api.Application.Shopping;

namespace Mohandseto.Api.Tests;

public sealed class CustomizationFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly CustomizationService _service;
    private readonly string _filesRoot = Path.Combine(Path.GetTempPath(), "mohandseto-tests", Guid.NewGuid().ToString("N"));
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Mohandseto.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public CustomizationFlowTests()
    {
        Directory.CreateDirectory(_filesRoot);
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated();
        CatalogSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        CustomizationSeeder.SeedAsync(_db, NullLogger.Instance).GetAwaiter().GetResult();
        _service = new CustomizationService(_db, _tenant, new TestEnvironment(_filesRoot));
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose();
        if (Directory.Exists(_filesRoot)) Directory.Delete(_filesRoot, true);
    }

    [Fact]
    public async Task Custom_product_runs_from_configuration_through_design_and_production()
    {
        var tenantId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var template = await _service.TemplateAsync((await _service.TemplatesAsync()).First().Id);
        var method = template.PrintMethods.OrderBy(x => x.PriceAdjustment).First();
        var form = new CreateCustomRequestForm
        {
            TemplateId = template.Id, Quantity = 99, PlacementId = template.Placements.First().Id,
            PrintMethodId = method.Id, MaterialId = template.Materials.First().Id, ColorId = template.Colors.First().Id,
            SizeId = template.Sizes.First().Id, DesignServiceRequested = true, Objective = "تجهيز هدايا مؤتمر الشركة",
            Audience = "عملاء الشركة", RequiredText = "شركة الاختبار", Logo = Upload("logo.png", "image/png"),
        };
        var created = await _service.CreateAsync(userId, form);
        Assert.Equal("AwaitingQuote", created.Status); Assert.Single(created.Assets); Assert.NotNull(created.DesignBrief);
        Assert.True(created.EstimatedTotal > created.EstimatedUnitPrice * created.Quantity);
        Assert.Single(await _service.SavedLogosAsync());

        var quote = await _service.SetQuoteAsync(created.Id, new SetCustomQuoteDto(created.EstimatedTotal - 100, DateTime.UtcNow.AddDays(5), 9));
        Assert.Equal("Quoted", quote.Status);
        var accepted = await _service.RespondToQuoteAsync(userId, created.Id, true);
        Assert.Equal("DesignInProgress", accepted.Status);

        var first = await _service.PublishDesignAsync(created.Id, Guid.NewGuid(), new PublishDesignForm
        { Title = "التصور الأول", File = Upload("mockup-1.png", "image/png") });
        Assert.Equal("AwaitingDesignApproval", first.Status); Assert.Equal(1, first.Versions.Single().VersionNumber);
        var revision = await _service.DesignDecisionAsync(userId, created.Id,
            new DesignDecisionDto(first.Versions.Single().Id, "RevisionRequested", "تكبير الشعار"));
        Assert.Equal("DesignInProgress", revision.Status);

        var second = await _service.PublishDesignAsync(created.Id, Guid.NewGuid(), new PublishDesignForm
        { Title = "التصور المعدل", ChangeSummary = "تم تكبير الشعار", File = Upload("mockup-2.png", "image/png") });
        Assert.Equal(2, second.Versions.First().VersionNumber);
        var approved = await _service.DesignDecisionAsync(userId, created.Id,
            new DesignDecisionDto(second.Versions.First().Id, "Approved", "معتمد للإنتاج"));
        Assert.Equal("DesignApproved", approved.Status); Assert.Null(approved.Production);
        var cart = new CartService(_db, _tenant);
        var cartResult = await cart.AddCustomRequestAsync(userId, created.Id);
        Assert.Equal(quote.QuotedTotal, cartResult.Total); Assert.Equal(created.Id, cartResult.Items.Single().CustomProductRequestId);
        Assert.Equal("AwaitingCheckout", (await _service.RequestAsync(userId, created.Id)).Status);
        await _service.StartForOrderAsync([created.Id], Guid.NewGuid(), false); await _db.SaveChangesAsync();
        var inProduction = await _service.RequestAsync(userId, created.Id);
        Assert.Equal("InProduction", inProduction.Status); Assert.Equal(6, inProduction.Production!.Stages.Count);

        var sample = await _service.PublishSampleAsync(created.Id, new PublishSampleForm
        { Note = "عينة فعلية قبل الإنتاج", File = Upload("sample.png", "image/png") });
        Assert.Equal("AwaitingSampleApproval", sample.Status); Assert.Single(sample.Production!.Samples);
        var sampleApproved = await _service.SampleDecisionAsync(userId, created.Id, sample.Production.Samples.Single().Id,
            new SampleDecisionDto("Approved", "العينة مطابقة"));
        Assert.Equal("InProduction", sampleApproved.Status);

        var current = sampleApproved;
        foreach (var stage in sampleApproved.Production!.Stages)
        {
            if (stage.Code == "quality")
                await _service.AddQualityCheckAsync(created.Id, Guid.NewGuid(), new AddQualityCheckDto("مطابقة الألوان والتشطيب", true, null));
            current = await _service.UpdateStageAsync(created.Id, stage.Id, new UpdateProductionStageDto("Completed", null));
        }
        Assert.Equal("Ready", current.Status);

        _tenant.TenantId = Guid.NewGuid();
        Assert.Empty(await _service.RequestsAsync(userId));
        await Assert.ThrowsAsync<ApiException>(() => _service.RequestAsync(userId, created.Id));
    }

    [Fact]
    public async Task Custom_product_validates_minimum_quantity_and_design_source()
    {
        _tenant.TenantId = Guid.NewGuid(); var userId = Guid.NewGuid();
        var template = await _service.TemplateAsync((await _service.TemplatesAsync()).First().Id);
        var baseForm = new CreateCustomRequestForm
        {
            TemplateId = template.Id, Quantity = 1, PlacementId = template.Placements.First().Id,
            PrintMethodId = template.PrintMethods.First().Id, MaterialId = template.Materials.First().Id,
            ColorId = template.Colors.First().Id, SizeId = template.Sizes.First().Id, Logo = Upload("logo.png", "image/png"),
            DesignFile = Upload("design.pdf", "application/pdf"),
        };
        var quantityError = await Assert.ThrowsAsync<ApiException>(() => _service.CreateAsync(userId, baseForm));
        Assert.Equal(400, quantityError.StatusCode);
        baseForm.Quantity = 100; baseForm.DesignFile = null;
        var sourceError = await Assert.ThrowsAsync<ApiException>(() => _service.CreateAsync(userId, baseForm));
        Assert.Contains("ملف التصميم", sourceError.Message);
    }

    private static IFormFile Upload(string name, string type)
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = type };
    }
}
