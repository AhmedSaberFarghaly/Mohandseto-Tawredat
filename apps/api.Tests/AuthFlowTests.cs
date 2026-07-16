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
    private readonly FakeExternalVerifier _external = new();

    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }

    private sealed class CapturingSms : ISmsSender
    {
        public string? LastMessage;
        public Task SendAsync(string phone, string message, CancellationToken ct = default)
        { LastMessage = message; return Task.CompletedTask; }
    }

    private sealed class FakeExternalVerifier : IExternalIdentityVerifier
    {
        public VerifiedExternalIdentity Identity { get; set; } = new("google", "google-subject", "owner@test.com", true, "Owner", null);
        public IReadOnlyList<ExternalProviderOptions> Providers { get; } =
        [
            new("google", "Google", true, "google-client", "https://accounts.example/.well-known/openid-configuration", "app:/oauth", ["openid", "email"], true),
            new("microsoft", "Microsoft", true, "microsoft-client", "https://login.example/.well-known/openid-configuration", "app:/oauth", ["openid", "email"], false, "organizations"),
        ];
        public ExternalProviderOptions Provider(string provider) => Providers.Single(x => x.Code == provider);
        public Task<VerifiedExternalIdentity> ValidateAsync(string provider, string idToken, string expectedNonce, CancellationToken ct = default)
        {
            Assert.Equal("valid-id-token", idToken);
            Assert.False(string.IsNullOrWhiteSpace(expectedNonce));
            Assert.Equal(provider, Identity.Provider);
            return Task.FromResult(Identity);
        }
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
        _auth = new AuthService(_db, new TokenService(config), _otp, external: _external);
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
    public async Task Google_oidc_challenge_auto_links_verified_email_and_cannot_be_replayed()
    {
        var registration = await RegisterCompanyAsync("01055500023", "social-google@test.com");
        _external.Identity = new("google", "google-123", "social-google@test.com", true, "Google Owner", null);
        var challenge = await _auth.BeginExternalAsync("google");
        var result = await _auth.LoginExternalAsync(new("google", "valid-id-token", challenge.ChallengeToken));
        Assert.NotNull(result.AccessToken);
        Assert.Equal(registration.User!.Id, result.User!.Id);
        Assert.True((await _db.ExternalIdentities.SingleAsync()).Email == "social-google@test.com");
        await Assert.ThrowsAsync<ApiException>(() => _auth.LoginExternalAsync(new("google", "valid-id-token", challenge.ChallengeToken)));
    }

    [Fact]
    public async Task Microsoft_requires_authenticated_link_then_supports_account_picker_login()
    {
        var registration = await RegisterCompanyAsync("01055500024", "social-ms@test.com");
        _external.Identity = new("microsoft", "entra-456", "social-ms@test.com", true, "Entra Owner", Guid.NewGuid().ToString());
        var first = await _auth.BeginExternalAsync("microsoft");
        var unlinked = await _auth.LoginExternalAsync(new("microsoft", "valid-id-token", first.ChallengeToken));
        Assert.True(unlinked.IsNewUser); Assert.Null(unlinked.AccessToken);

        var linkChallenge = await _auth.BeginExternalAsync("microsoft");
        var link = await _auth.LinkExternalAsync(registration.User!.Id, new("microsoft", "valid-id-token", linkChallenge.ChallengeToken));
        Assert.Equal("microsoft", link.Provider);

        var loginChallenge = await _auth.BeginExternalAsync("microsoft");
        var login = await _auth.LoginExternalAsync(new("microsoft", "valid-id-token", loginChallenge.ChallengeToken));
        Assert.NotNull(login.AccessToken); Assert.Equal(registration.User.Id, login.User!.Id);
    }

    [Fact]
    public async Task Unknown_verified_google_identity_enters_registration_path_without_creating_user()
    {
        _external.Identity = new("google", "new-google-user", "new-social@test.com", true, "New User", null);
        var challenge = await _auth.BeginExternalAsync("google");
        var result = await _auth.LoginExternalAsync(new("google", "valid-id-token", challenge.ChallengeToken));
        Assert.True(result.IsNewUser); Assert.Equal("new-social@test.com", result.PrefillEmail);
        Assert.False(await _db.ExternalIdentities.AnyAsync());
    }

    [Fact]
    public async Task Password_reset_uses_otp_consumes_challenge_and_revokes_sessions()
    {
        var registration = await RegisterCompanyAsync("01055500014", "reset@test.com");
        var request = await _auth.RequestPasswordResetAsync("reset@test.com");
        Assert.True(request.Sent); Assert.NotNull(request.DevelopmentCode); Assert.Equal("رقم الهاتف المسجل", request.MaskedPhone);

        await _auth.ResetPasswordAsync(new PasswordResetDto(request.ResetToken, request.DevelopmentCode!, "NewSecret@456"));
        await Assert.ThrowsAsync<ApiException>(() => _auth.RefreshAsync(registration.RefreshToken!));
        await Assert.ThrowsAsync<ApiException>(() => _auth.ResetPasswordAsync(
            new PasswordResetDto(request.ResetToken, request.DevelopmentCode!, "Another@789")));
        var login = await _auth.LoginWithEmailAsync("reset@test.com", "NewSecret@456");
        Assert.NotNull(login.AccessToken);
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
