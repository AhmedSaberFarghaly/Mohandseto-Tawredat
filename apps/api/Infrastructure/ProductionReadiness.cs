namespace Mohandseto.Api.Infrastructure;

/// <summary>Fail-fast checks for configuration mistakes that would make a public deployment unsafe.</summary>
public static class ProductionReadiness
{
    private const string DevelopmentKeyMarker = "DEV-ONLY-KEY";

    public static IReadOnlyList<string> Validate(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction) return [];
        var errors = new List<string>();
        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 48 || jwtKey.Contains(DevelopmentKeyMarker, StringComparison.OrdinalIgnoreCase))
            errors.Add("Jwt:Key must be a production secret of at least 48 characters.");

        var connection = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connection)) errors.Add("ConnectionStrings:Default is required.");

        var hosts = configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(hosts) || hosts.Trim() == "*") errors.Add("AllowedHosts must contain the public API host and cannot be '*'.");

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (origins.Length == 0) errors.Add("At least one Cors:Origins entry is required.");
        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback)
                errors.Add($"Production CORS origin must be a non-loopback HTTPS URL: {origin}");
        }

        if (configuration.GetValue("Seed:Enabled", false)) errors.Add("Seed:Enabled must be false in production.");
        var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        if (knownProxies.Length == 0 || knownProxies.Any(value => !System.Net.IPAddress.TryParse(value, out _)))
            errors.Add("ForwardedHeaders:KnownProxies must contain the trusted reverse-proxy IP address(es).");
        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath) || !Path.IsPathRooted(keysPath)) errors.Add("DataProtection:KeysPath must be an absolute persistent path in production.");
        return errors;
    }

    public static void ThrowIfInvalid(IConfiguration configuration, IHostEnvironment environment)
    {
        var errors = Validate(configuration, environment.IsProduction());
        if (errors.Count > 0) throw new InvalidOperationException("Unsafe production configuration:\n - " + string.Join("\n - ", errors));
    }
}

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        if (!environment.IsDevelopment())
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
        await next(context);
    }
}
