using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class ProductionReadinessTests
{
    [Fact]
    public void Production_rejects_placeholders_wildcard_hosts_http_origins_and_ephemeral_keys()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "DEV-ONLY-KEY-change-me",
            ["ConnectionStrings:Default"] = "Data Source=app.db",
            ["AllowedHosts"] = "*",
            ["Cors:Origins:0"] = "http://localhost:3000",
            ["Seed:Enabled"] = "true",
            ["DataProtection:KeysPath"] = "keys",
        });

        var errors = ProductionReadiness.Validate(configuration, true);

        Assert.Equal(6, errors.Count);
        Assert.Empty(ProductionReadiness.Validate(configuration, false));
    }

    [Fact]
    public void Production_accepts_explicit_secure_configuration()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "a-production-secret-with-at-least-forty-eight-characters-123456789",
            ["ConnectionStrings:Default"] = "Data Source=/var/lib/mohandseto/app.db",
            ["AllowedHosts"] = "api.example.com",
            ["Cors:Origins:0"] = "https://admin.example.com",
            ["Seed:Enabled"] = "false",
            ["ForwardedHeaders:KnownProxies:0"] = "172.31.50.1",
            ["DataProtection:KeysPath"] = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mohandseto-keys")),
        });

        Assert.Empty(ProductionReadiness.Validate(configuration, true));
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}

public sealed class MigrationIntegrityTests
{
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }

    [Fact]
    public async Task All_migrations_apply_from_an_empty_database()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mohandseto-migrations-{Guid.NewGuid():N}.db");
        try
        {
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options, new PlatformTenant());
            await db.Database.MigrateAsync();
            var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
            Assert.True(applied.Count >= 35);
            Assert.EndsWith("AddSystemMonitoring", applied[^1]);
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Users','Products','Orders','SystemErrorEvents','FeatureFlags')";
            if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync();
            Assert.Equal(5L, (long)(await command.ExecuteScalarAsync())!);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

public sealed class ApiE2eTests : IClassFixture<ApiE2eFactory>
{
    private readonly HttpClient _client;
    public ApiE2eTests(ApiE2eFactory factory) => _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Admin_login_catalog_monitoring_and_security_headers_work_over_http()
    {
        var live = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("nosniff", live.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", live.Headers.GetValues("X-Frame-Options").Single());

        var ready = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var anonymous = await _client.GetAsync("/api/admin/monitoring");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email = "release-admin@mohandseto.test", password = "Release@12345" });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = auth.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var catalog = await _client.GetFromJsonAsync<JsonElement>("/api/catalog/categories");
        Assert.True(catalog.GetArrayLength() >= 12);
        var monitoring = await _client.GetFromJsonAsync<JsonElement>("/api/admin/monitoring");
        Assert.Equal("Healthy", monitoring.GetProperty("database").GetProperty("status").GetString());
        Assert.Equal(4, monitoring.GetProperty("queues").GetArrayLength());
    }
}

public sealed class ApiE2eFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"mohandseto-e2e-{Guid.NewGuid():N}");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_root);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = $"Data Source={Path.Combine(_root, "e2e.db")}",
            ["DataProtection:KeysPath"] = Path.Combine(_root, "keys"),
            ["Seed:AdminEmail"] = "release-admin@mohandseto.test",
            ["Seed:AdminPassword"] = "Release@12345",
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
