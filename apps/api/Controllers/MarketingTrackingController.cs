using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminMarketing;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/marketing"), Authorize]
public sealed class MarketingTrackingController(AdminMarketingService marketing) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw ApiException.Unauthorized();

    [HttpPost("campaigns/{id:guid}/events")]
    public async Task<IActionResult> Track(Guid id, TrackMarketingEventDto dto, CancellationToken ct)
    {
        await marketing.TrackAsync(id, UserId, dto, ct); return NoContent();
    }
}
