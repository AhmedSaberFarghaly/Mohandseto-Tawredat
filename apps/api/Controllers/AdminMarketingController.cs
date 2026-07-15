using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminMarketing;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/marketing"), Authorize(Roles = "super_admin,system_admin,marketing_manager")]
public sealed class AdminMarketingController(AdminMarketingService marketing) : ControllerBase
{
    [HttpGet] public Task<AdminMarketingDashboardDto> Dashboard(CancellationToken ct) => marketing.DashboardAsync(ct);
    [HttpGet("campaigns/{id:guid}")] public Task<MarketingCampaignDto> Campaign(Guid id, CancellationToken ct) => marketing.GetCampaignAsync(id, ct);
    [HttpPost("campaigns")] public Task<MarketingCampaignDto> CreateCampaign(SaveMarketingCampaignDto dto, CancellationToken ct) => marketing.CreateCampaignAsync(dto, ct);
    [HttpPost("campaigns/{id:guid}/send")] public Task<MarketingCampaignDto> Send(Guid id, CancellationToken ct) => marketing.SendAsync(id, ct);
    [HttpPost("campaigns/{id:guid}/cancel")] public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) { await marketing.CancelAsync(id, ct); return NoContent(); }
    [HttpPost("campaigns/process-due")] public async Task<IActionResult> ProcessDue(CancellationToken ct) => Ok(new { count = await marketing.ProcessDueAsync(ct) });
    [HttpPost("coupons")] public Task<MarketingCouponDto> SaveCoupon(SaveMarketingCouponDto dto, CancellationToken ct) => marketing.SaveCouponAsync(dto, ct);
    [HttpPut("coupons/{groupId:guid}/state")] public async Task<IActionResult> CouponState(Guid groupId, [FromQuery] bool active, CancellationToken ct) { await marketing.SetCouponStateAsync(groupId, active, ct); return NoContent(); }
}

