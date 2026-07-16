using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Application.Auth;

public sealed record ExternalProviderOptions(
    string Code,
    string DisplayName,
    bool Enabled,
    string ClientId,
    string DiscoveryUrl,
    string RedirectUrl,
    IReadOnlyList<string> Scopes,
    bool AllowEmailAutoLink,
    string? Tenant = null,
    string? IosClientId = null);

public sealed record VerifiedExternalIdentity(
    string Provider,
    string Subject,
    string? Email,
    bool EmailVerified,
    string? DisplayName,
    string? ProviderTenantId);

public interface IExternalIdentityVerifier
{
    IReadOnlyList<ExternalProviderOptions> Providers { get; }
    ExternalProviderOptions Provider(string provider);
    Task<VerifiedExternalIdentity> ValidateAsync(string provider, string idToken, string expectedNonce, CancellationToken ct = default);
}

/// <summary>Validates Google/Microsoft ID tokens locally using cached OIDC discovery signing keys.</summary>
public sealed class ExternalIdentityVerifier(IConfiguration configuration, ILogger<ExternalIdentityVerifier> logger) : IExternalIdentityVerifier
{
    private const string GoogleDiscovery = "https://accounts.google.com/.well-known/openid-configuration";
    private const string MicrosoftConsumerTenant = "9188040d-6c67-4c5b-b112-36a304b66dad";
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _managers = new();

    public IReadOnlyList<ExternalProviderOptions> Providers { get; } = BuildProviders(configuration);

    public ExternalProviderOptions Provider(string provider) => Providers.FirstOrDefault(x => x.Code.Equals(provider, StringComparison.OrdinalIgnoreCase))
        ?? throw ApiException.BadRequest("مزود تسجيل الدخول غير مدعوم");

    public async Task<VerifiedExternalIdentity> ValidateAsync(string provider, string idToken, string expectedNonce, CancellationToken ct = default)
    {
        var options = Provider(provider);
        if (!options.Enabled) throw ApiException.BadRequest($"تسجيل الدخول عبر {options.DisplayName} غير مهيأ بعد");
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > 16_384) throw ApiException.Unauthorized("رمز الهوية الخارجي غير صالح");

        try
        {
            var manager = _managers.GetOrAdd(options.Code, _ => new ConfigurationManager<OpenIdConnectConfiguration>(
                options.DiscoveryUrl,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = true }));
            var metadata = await manager.GetConfigurationAsync(ct);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = metadata.SigningKeys,
                ValidateAudience = true,
                ValidAudience = options.ClientId,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                ValidateIssuer = options.Code != "microsoft",
                ValidIssuers = options.Code == "google" ? ["https://accounts.google.com", "accounts.google.com"] : null,
            };
            var handler = new JsonWebTokenHandler { MapInboundClaims = false };
            var result = await handler.ValidateTokenAsync(idToken, parameters);
            if (!result.IsValid || result.ClaimsIdentity is null) throw result.Exception ?? new SecurityTokenValidationException();

            var identity = result.ClaimsIdentity;
            var subject = Claim(identity, "sub") ?? throw new SecurityTokenValidationException("Missing subject");
            if (options.Code == "microsoft")
            {
                var nonce = Claim(identity, "nonce");
                if (string.IsNullOrWhiteSpace(nonce) || !CryptographicOperations.FixedTimeEquals(
                        System.Text.Encoding.UTF8.GetBytes(nonce), System.Text.Encoding.UTF8.GetBytes(expectedNonce)))
                    throw new SecurityTokenValidationException("Nonce mismatch");
            }

            string? providerTenant = null;
            if (options.Code == "microsoft")
            {
                providerTenant = Claim(identity, "tid") ?? throw new SecurityTokenValidationException("Missing tenant");
                ValidateMicrosoftIssuer(result.SecurityToken.Issuer, providerTenant, options.Tenant);
            }

            var email = Claim(identity, "email")?.Trim().ToLowerInvariant();
            var emailVerified = options.Code == "google"
                ? bool.TryParse(Claim(identity, "email_verified"), out var verified) && verified
                : email is not null;
            return new(options.Code, subject, email, emailVerified, Claim(identity, "name"), providerTenant);
        }
        catch (Exception exception) when (exception is not ApiException)
        {
            logger.LogWarning("External {Provider} token validation failed: {FailureType}", options.Code, exception.GetType().Name);
            throw ApiException.Unauthorized("تعذر التحقق من حساب مزود تسجيل الدخول");
        }
    }

    private static string? Claim(ClaimsIdentity identity, string type) => identity.FindFirst(type)?.Value;

    private static void ValidateMicrosoftIssuer(string issuer, string tenantId, string? allowedTenant)
    {
        if (!Guid.TryParse(tenantId, out _)) throw new SecurityTokenInvalidIssuerException();
        var expected = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        if (!issuer.Equals(expected, StringComparison.OrdinalIgnoreCase)) throw new SecurityTokenInvalidIssuerException();
        if (allowedTenant?.Equals("organizations", StringComparison.OrdinalIgnoreCase) == true && tenantId.Equals(MicrosoftConsumerTenant, StringComparison.OrdinalIgnoreCase))
            throw new SecurityTokenInvalidIssuerException();
        if (Guid.TryParse(allowedTenant, out var configured) && !configured.Equals(Guid.Parse(tenantId)))
            throw new SecurityTokenInvalidIssuerException();
    }

    private static IReadOnlyList<ExternalProviderOptions> BuildProviders(IConfiguration configuration)
    {
        var redirect = configuration["ExternalAuth:RedirectUrl"] ?? "com.mohandseto.tawredat:/oauthredirect";
        var microsoftTenant = configuration["ExternalAuth:Microsoft:Tenant"] ?? "organizations";
        var googleClient = configuration["ExternalAuth:Google:ClientId"]?.Trim() ?? string.Empty;
        var googleIosClient = configuration["ExternalAuth:Google:IosClientId"]?.Trim();
        var microsoftClient = configuration["ExternalAuth:Microsoft:ClientId"]?.Trim() ?? string.Empty;
        return
        [
            new("google", "Google", googleClient.Length > 0, googleClient, GoogleDiscovery, redirect,
                ["openid", "profile", "email"], configuration.GetValue("ExternalAuth:Google:AllowEmailAutoLink", false), IosClientId: googleIosClient),
            new("microsoft", "Microsoft", microsoftClient.Length > 0, microsoftClient,
                $"https://login.microsoftonline.com/{Uri.EscapeDataString(microsoftTenant)}/v2.0/.well-known/openid-configuration",
                redirect, ["openid", "profile", "email"], configuration.GetValue("ExternalAuth:Microsoft:AllowEmailAutoLink", false), microsoftTenant),
        ];
    }
}
