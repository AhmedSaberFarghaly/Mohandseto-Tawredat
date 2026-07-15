using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
using Mohandseto.Api.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

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
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddPolicy("app", p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
builder.Services.AddProblemDetails();

// application services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
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
builder.Services.AddHostedService<ContentDispatchWorker>();
builder.Services.AddHostedService<ContractPriceRevisionWorker>();
builder.Services.AddScoped<CustomizationService>();
builder.Services.AddSingleton<ISmsSender, ConsoleSmsSender>();

// brute-force protection on auth endpoints
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseSerilogRequestLogging();

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
app.UseStatusCodePages();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("app");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { name = "Mohandseto Tawredat API", version = "0.1.0", status = "ok" }));

// apply migrations + seed base data automatically in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db, app.Configuration, app.Logger);
}

app.Run();
