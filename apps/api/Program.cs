using System.Text;
using System.IO.Compression;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Mohandseto.Api.Application.Auth;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Application.Approvals;
using Mohandseto.Api.Application.Rfq;
using Mohandseto.Api.Application.Orders;
using Mohandseto.Api.Application.Returns;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Application.Budgets;
using Mohandseto.Api.Application.Account;
using Mohandseto.Api.Application.Engagement;
using Mohandseto.Api.Application.Admin;
using Mohandseto.Api.Application.AdminOrders;
using Mohandseto.Api.Application.AdminQuotes;
using Mohandseto.Api.Application.AdminContent;
using Mohandseto.Api.Application.AdminInventory;
using Mohandseto.Api.Application.AdminProcurement;
using Mohandseto.Api.Application.AdminCrm;
using Mohandseto.Api.Application.AdminContracts;
using Mohandseto.Api.Application.AdminPrinting;
using Mohandseto.Api.Application.AdminShipping;
using Mohandseto.Api.Application.AdminMarketing;
using Mohandseto.Api.Application.AdminSystemAccess;
using Mohandseto.Api.Application.AdminReports;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Application.AdminIntegrations;
using Mohandseto.Api.Application.AdminMonitoring;
using Mohandseto.Api.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 30 * 1024 * 1024);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

builder.Services.AddSingleton<RuntimeMetrics>();
builder.Services.AddSingleton<DatabaseMetricsInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, opt) =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default"))
        .AddInterceptors(services.GetRequiredService<DatabaseMetricsInterceptor>()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing"))),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        opt.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var raw = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.Principal?.FindFirst("sub")?.Value;
                if (!Guid.TryParse(raw, out var userId)) { context.Fail("Invalid user"); return; }
                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, context.HttpContext.RequestAborted);
                if (user is null || !user.IsActive && (user.SuspendedUntil is null || user.SuspendedUntil > DateTime.UtcNow))
                    context.Fail("Inactive user");
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddPolicy("app", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);
builder.Services.AddProblemDetails();
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Mohandseto.Tawredat");
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var raw in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        if (IPAddress.TryParse(raw, out var address)) options.KnownProxies.Add(address);
});

// application services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<IExternalIdentityVerifier, ExternalIdentityVerifier>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<AdminCatalogOperationsService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddScoped<PaymentGatewayService>();
builder.Services.AddScoped<ApprovalService>();
builder.Services.AddScoped<RfqService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ReturnService>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SupportService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<AdminOrderService>();
builder.Services.AddScoped<AdminQuoteService>();
builder.Services.AddScoped<AdminContentService>();
builder.Services.AddScoped<AdminInventoryService>();
builder.Services.AddScoped<AdminProcurementService>();
builder.Services.AddScoped<AdminCrmService>();
builder.Services.AddScoped<AdminContractService>();
builder.Services.AddScoped<AdminPrintingService>();
builder.Services.AddScoped<AdminShippingService>();
builder.Services.AddScoped<AdminMarketingService>();
builder.Services.AddScoped<AdminSystemAccessService>();
builder.Services.AddScoped<AdminReportService>();
builder.Services.AddScoped<AdminSystemSettingsService>();
builder.Services.AddScoped<AdminIntegrationService>();
builder.Services.AddScoped<AdminMonitoringService>();
builder.Services.AddSingleton<IReportDeliverySender, ConsoleReportDeliverySender>();
builder.Services.AddSingleton<IMarketingChannelSender, ConsoleMarketingChannelSender>();
builder.Services.AddScoped<Mohandseto.Api.Application.AdminAccounting.AdminAccountingService>();
builder.Services.AddScoped<Mohandseto.Api.Application.AdminCustomerService.AdminCustomerServiceService>();
builder.Services.AddHostedService<ContentDispatchWorker>();
builder.Services.AddHostedService<MarketingCampaignWorker>();
builder.Services.AddHostedService<ContractPriceRevisionWorker>();
builder.Services.AddHostedService<ScheduledReportWorker>();
builder.Services.AddHostedService<ScheduledSystemBackupWorker>();
builder.Services.AddHostedService<IntegrationRetryWorker>();
builder.Services.AddScoped<CustomizationService>();
builder.Services.AddSingleton<ISmsSender, ConsoleSmsSender>();

// brute-force protection on auth endpoints
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 600,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
ProductionReadiness.ThrowIfInvalid(app.Configuration, app.Environment);

app.UseForwardedHeaders();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseResponseCompression();

// map ApiException -> ProblemDetails with Arabic message; hide stack traces
app.UseExceptionHandler(handler => handler.Run(async ctx =>
{
    var feature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var (status, title, code) = feature?.Error is ApiException apiEx
        ? (apiEx.StatusCode, apiEx.Message, apiEx.Code)
        : (500, "حدث خطأ غير متوقع، حاول مرة أخرى", (string?)null);
    if (status == 500 && feature?.Error is not null)
        app.Logger.LogError(feature.Error, "Unhandled exception");
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new
    {
        type = "about:blank",
        title,
        status,
        code,
        traceId = ctx.TraceIdentifier,
    });
}));
app.UseMiddleware<SystemErrorCaptureMiddleware>();
app.UseMiddleware<RequestMetricsMiddleware>();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("app");
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers.CacheControl = "public,max-age=86400",
});
app.UseMiddleware<BlockedIpMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new() { Predicate = check => check.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
app.MapGet("/", () => Results.Ok(new { name = "Mohandseto Tawredat API", version = "1.0.0-rc.1", status = "ok" }));

// Development and single-instance staging can opt into startup migration. Multi-replica production runs it as a release step.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Seed:Enabled", false))
        await DbSeeder.SeedAsync(db, app.Configuration, app.Logger);
}

app.Run();

public partial class Program;
