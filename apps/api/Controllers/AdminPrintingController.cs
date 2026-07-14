using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminPrinting;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize(Roles = "super_admin,graphic_designer,printing_officer,operations_manager"), Route("api/admin/printing")]
public sealed class AdminPrintingController(AdminPrintingService printing) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();

    [HttpGet] public Task<PrintingDashboardDto> Dashboard(CancellationToken ct) => printing.DashboardAsync(ct);
    [HttpGet("requests/{id:guid}")] public Task<PrintingRequestDetailDto> Detail(Guid id, CancellationToken ct) => printing.DetailAsync(id, ct);
    [HttpPut("requests/{id:guid}/designer")] public async Task<IActionResult> Assign(Guid id, AssignPrintingDesignerDto dto, CancellationToken ct) { await printing.AssignDesignerAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPut("requests/{requestId:guid}/logos/{assetId:guid}/quality")] public async Task<IActionResult> ReviewLogo(Guid requestId, Guid assetId, ReviewLogoQualityDto dto, CancellationToken ct) { await printing.ReviewLogoAsync(UserId, requestId, assetId, dto, ct); return NoContent(); }
    [HttpPost("requests/{id:guid}/send-design")] public async Task<IActionResult> SendDesign(Guid id, SendDesignToCustomerDto dto, CancellationToken ct) { await printing.SendDesignAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("requests/{id:guid}/internal-comments")] public async Task<IActionResult> Comment(Guid id, AddInternalPrintingCommentDto dto, CancellationToken ct) { await printing.AddInternalCommentAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("requests/{id:guid}/start-production")] public async Task<IActionResult> Start(Guid id, StartPrintingProductionDto dto, CancellationToken ct) { await printing.StartProductionAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPut("requests/{requestId:guid}/stages/{stageId:guid}")] public async Task<IActionResult> Stage(Guid requestId, Guid stageId, UpdatePrintingStageDto dto, CancellationToken ct) { await printing.UpdateStageAsync(UserId, requestId, stageId, dto, ct); return NoContent(); }
    [HttpPut("requests/{id:guid}/packaging")] public async Task<IActionResult> Packaging(Guid id, SavePackagingDto dto, CancellationToken ct) { await printing.SavePackagingAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("requests/{id:guid}/ready")] public async Task<IActionResult> Ready(Guid id, MarkPrintingReadyDto dto, CancellationToken ct) { await printing.MarkReadyAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPut("templates/{id:guid}")] public async Task<IActionResult> Template(Guid id, SavePrintingTemplateDto dto, CancellationToken ct) { await printing.UpdateTemplateAsync(UserId, id, dto, ct); return NoContent(); }
}
