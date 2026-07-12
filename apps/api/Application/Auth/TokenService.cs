using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Application.Auth;

public record TokenPair(string AccessToken, DateTime AccessExpiresAt, string RefreshToken, DateTime RefreshExpiresAt);

public class TokenService(IConfiguration config)
{
    public TokenPair Issue(User user, IEnumerable<string> roleCodes)
    {
        var jwt = config.GetSection("Jwt");
        var accessMinutes = jwt.GetValue("AccessTokenMinutes", 30);
        var refreshDays = jwt.GetValue("RefreshTokenDays", 30);
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(accessMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.FullName),
            new("staff", user.IsPlatformStaff ? "1" : "0"),
        };
        if (user.TenantId is { } tid) claims.Add(new Claim("tenant_id", tid.ToString()));
        claims.AddRange(roleCodes.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var refreshRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        return new TokenPair(
            new JwtSecurityTokenHandler().WriteToken(token),
            accessExpires,
            refreshRaw,
            now.AddDays(refreshDays));
    }

    public static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
