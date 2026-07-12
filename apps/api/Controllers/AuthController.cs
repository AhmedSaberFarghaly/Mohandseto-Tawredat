using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Mohandseto.Api.Application.Auth;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth, OtpService otp) : ControllerBase
{
    [HttpPost("otp/request")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RequestOtp(OtpRequestDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<OtpPurpose>(dto.Purpose, ignoreCase: true, out var purpose))
            throw ApiException.BadRequest("غرض الرمز غير صحيح");
        var devCode = await otp.RequestAsync(dto.Phone, purpose, ct);
        return Ok(new { sent = true, devCode });
    }

    [HttpPost("otp/verify")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResultDto>> VerifyOtp(OtpVerifyDto dto, CancellationToken ct) =>
        Ok(await auth.LoginWithOtpAsync(dto.Phone, dto.Code, ct));

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResultDto>> Login(EmailLoginDto dto, CancellationToken ct) =>
        Ok(await auth.LoginWithEmailAsync(dto.Email, dto.Password, ct));

    [HttpPost("register-company")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResultDto>> RegisterCompany(RegisterCompanyDto dto, CancellationToken ct) =>
        Ok(await auth.RegisterCompanyAsync(dto, ct));

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshDto dto, CancellationToken ct) =>
        Ok(await auth.RefreshAsync(dto.RefreshToken, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshDto dto, CancellationToken ct)
    {
        await auth.LogoutAsync(dto.RefreshToken, ct);
        return Ok(new { loggedOut = true });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me(CancellationToken ct)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw ApiException.Unauthorized());
        return Ok(await auth.MeAsync(id, ct));
    }
}
