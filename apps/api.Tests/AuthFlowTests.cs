using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Mohandseto.Api.Application.Auth;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

/// <summary>Service-level tests over a real SQLite in-memory database (no mocks of EF behavior).</summary>
public class AuthFlowTests : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly AppDbContext _db;
    private readonly OtpService _otp;
    private readonly AuthService _auth;
    private readonly CapturingSms _sms = new();
    private readonly TestTenantProvider _tenant = new();

    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }

    private sealed class CapturingSms : ISmsSender
    {
        public string? LastMessage;
        public Task SendAsync(string phone, string message, CancellationToken ct = default)
        { LastMessage = message; return Task.CompletedTask; }
    }

    private sealed class DevEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = ".";
        public string EnvironmentName { get; set; } = "Development";
    }

    public AuthFlowTests()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        _db = new AppDbContext(options, _tenant);
        _db.Database.EnsureCreated();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:Key"] = "test-key-0123456789-0123456789-0123456789-0123456789",
            ["Jwt:AccessTokenMinutes"] = "30",
            ["Jwt:RefreshTokenDays"] = "30",
        }).Build();

        DbSeeder.SeedAsync(_db, config, NullLogger.Instance).GetAwaiter().GetResult();
        _otp = new OtpService(_db, _sms, new DevEnv());
        _auth = new AuthService(_db, new TokenService(config), _otp);
    }

    public void Dispose() { _db.Dispose(); _conn.Dispose(); }

    private async Task<AuthResultDto> RegisterCompanyAsync(string phone, string email)
    {
        var code = (await _otp.RequestAsync(phone, OtpPurpose.Registration))!;
        return await _auth.RegisterCompanyAsync(new RegisterCompanyDto(
            phone, code, "شركة الاختبار", null, null, null, "القاهرة", null, null, null,
            "مستخدم الاختبار", email, "Secret@123"));
    }

    [Fact]
    public async Task Otp_request_and_verify_succeeds_and_consumes_code()
    {
        var code = await _otp.RequestAsync("01011112222", OtpPurpose.Login);
        Assert.NotNull(code);
        Assert.Contains(code!, _sms.LastMessage);
        await _otp.VerifyAsync("01011112222", code!, OtpPurpose.Login);
        // consumed: second verify must fail
        await Assert.ThrowsAsync<ApiException>(() => _otp.VerifyAsync("01011112222", code!, OtpPurpose.Login));
    }

    [Fact]
    public async Task Otp_wrong_code_fails()
    {
        await _otp.RequestAsync("01011113333", OtpPurpose.Login);
        var ex = await Assert.ThrowsAsync<ApiException>(() => _otp.VerifyAsync("01011113333", "000000", OtpPurpose.Login));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Otp_rate_limit_blocks_fourth_request_in_window()
    {
        for (var i = 0; i < 3; i++) await _otp.RequestAsync("01011114444", OtpPurpose.Login);
        var ex = await Assert.ThrowsAsync<ApiException>(() => _otp.RequestAsync("01011114444", OtpPurpose.Login));
        Assert.Equal(429, ex.StatusCode);
    }

    [Fact]
    public async Task Company_registration_creates_tenant_company_branch_owner()
    {
        var result = await RegisterCompanyAsync("01055512345", "owner@test.com");
        Assert.False(result.IsNewUser);
        Assert.NotNull(result.AccessToken);
        Assert.Equal("PendingVerification", result.User!.TenantStatus);
        Assert.Contains("company_owner", result.User.Roles);
        Assert.Equal(1, await _db.Companies.CountAsync());
        Assert.Equal(1, await _db.CompanyBranches.IgnoreQueryFilters().CountAsync(b => b.IsMain));
    }

    [Fact]
    public async Task Duplicate_phone_registration_conflicts()
    {
        await RegisterCompanyAsync("01055500001", "a@test.com");
        var code = (await _otp.RequestAsync("01055500001", OtpPurpose.Registration))!;
        var ex = await Assert.ThrowsAsync<ApiException>(() => _auth.RegisterCompanyAsync(new RegisterCompanyDto(
            "01055500001", code, "شركة أخرى", null, null, null, "الجيزة", null, null, null,
            "شخص آخر", "b@test.com", "Secret@123")));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotation_blocks_reuse_and_revokes_all_sessions()
    {
        var reg = await RegisterCompanyAsync("01055500002", "c@test.com");
        var rotated = await _auth.RefreshAsync(reg.RefreshToken!);
        Assert.NotNull(rotated.AccessToken);

        // reuse of the already-rotated token → 401 and all sessions revoked
        await Assert.ThrowsAsync<ApiException>(() => _auth.RefreshAsync(reg.RefreshToken!));
        await Assert.ThrowsAsync<ApiException>(() => _auth.RefreshAsync(rotated.RefreshToken!));
    }

    [Fact]
    public async Task Email_login_with_wrong_password_fails()
    {
        await RegisterCompanyAsync("01055500003", "d@test.com");
        var ex = await Assert.ThrowsAsync<ApiException>(() => _auth.LoginWithEmailAsync("d@test.com", "WRONG"));
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task Email_login_requires_and_verifies_second_factor_when_enabled()
    {
        var registration = await RegisterCompanyAsync("01055500013", "twofactor@test.com");
        var user = await _db.Users.SingleAsync(u => u.Id == registration.User!.Id);
        user.TwoFactorEnabled = true; user.TwoFactorChannel = "sms"; await _db.SaveChangesAsync();
        var challenge = await _auth.LoginWithEmailAsync("twofactor@test.com", "Secret@123");
        Assert.True(challenge.RequiresTwoFactor); Assert.Null(challenge.AccessToken); Assert.NotNull(challenge.ChallengeToken); Assert.NotNull(challenge.DevelopmentCode);
        var verified = await _auth.VerifyTwoFactorAsync(new TwoFactorLoginDto(challenge.ChallengeToken!, challenge.DevelopmentCode!));
        Assert.NotNull(verified.AccessToken); Assert.False(verified.RequiresTwoFactor);
        await Assert.ThrowsAsync<ApiException>(() => _auth.VerifyTwoFactorAsync(new TwoFactorLoginDto(challenge.ChallengeToken!, challenge.DevelopmentCode!)));
    }

    [Fact]
    public async Task Tenant_isolation_filter_hides_other_tenant_branches()
    {
        var a = await RegisterCompanyAsync("01055500004", "e@test.com");
        var b = await RegisterCompanyAsync("01055500005", "f@test.com");

        _tenant.TenantId = a.User!.TenantId;
        var visible = await _db.CompanyBranches.ToListAsync();
        Assert.All(visible, br => Assert.Equal(a.User.TenantId, br.TenantId));

        _tenant.TenantId = b.User!.TenantId;
        visible = await _db.CompanyBranches.ToListAsync();
        Assert.All(visible, br => Assert.Equal(b.User.TenantId, br.TenantId));
    }

    [Fact]
    public async Task Seeder_creates_roles_permissions_and_super_admin()
    {
        Assert.True(await _db.Roles.CountAsync() >= 25);
        Assert.True(await _db.Permissions.CountAsync() >= 30);
        var admin = await _db.Users.FirstOrDefaultAsync(u => u.Email == "admin@mohandseto.com");
        Assert.NotNull(admin);
        Assert.True(admin!.IsPlatformStaff);
    }
}
