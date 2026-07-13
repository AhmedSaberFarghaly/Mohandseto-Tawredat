using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminContent;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/content"), Authorize(Roles = "super_admin,system_admin,content_manager,marketing_manager")]
public sealed class AdminContentController(AdminContentService service) : ControllerBase
{
    [HttpGet] public Task<AdminContentDashboardDto> Dashboard(CancellationToken ct) => service.DashboardAsync(ct);

    [HttpPost("sections")] public Task<HomeSectionDto> CreateSection(SaveHomeSectionDto dto, CancellationToken ct) => service.SaveSectionAsync(null, dto, ct);
    [HttpPut("sections/{id:guid}")] public Task<HomeSectionDto> UpdateSection(Guid id, SaveHomeSectionDto dto, CancellationToken ct) => service.SaveSectionAsync(id, dto, ct);
    [HttpPut("sections/reorder")] public async Task<IActionResult> ReorderSections(ReorderDto dto, CancellationToken ct) { await service.ReorderSectionsAsync(dto, ct); return NoContent(); }
    [HttpDelete("sections/{id:guid}")] public async Task<IActionResult> DeleteSection(Guid id, CancellationToken ct) { await service.DeleteSectionAsync(id, ct); return NoContent(); }

    [HttpPost("banners")] public Task<HomeBannerDto> CreateBanner(SaveHomeBannerDto dto, CancellationToken ct) => service.SaveBannerAsync(null, dto, ct);
    [HttpPut("banners/{id:guid}")] public Task<HomeBannerDto> UpdateBanner(Guid id, SaveHomeBannerDto dto, CancellationToken ct) => service.SaveBannerAsync(id, dto, ct);
    [HttpDelete("banners/{id:guid}")] public async Task<IActionResult> DeleteBanner(Guid id, CancellationToken ct) { await service.DeleteBannerAsync(id, ct); return NoContent(); }

    [HttpPost("pages")] public Task<ContentPageAdminDto> CreatePage(SaveContentPageDto dto, CancellationToken ct) => service.SavePageAsync(null, dto, ct);
    [HttpPut("pages/{id:guid}")] public Task<ContentPageAdminDto> UpdatePage(Guid id, SaveContentPageDto dto, CancellationToken ct) => service.SavePageAsync(id, dto, ct);
    [HttpDelete("pages/{id:guid}")] public async Task<IActionResult> DeletePage(Guid id, CancellationToken ct) { await service.DeletePageAsync(id, ct); return NoContent(); }

    [HttpPost("faq")] public Task<SupportArticleAdminDto> CreateFaq(SaveSupportArticleDto dto, CancellationToken ct) => service.SaveFaqAsync(null, dto, ct);
    [HttpPut("faq/{id:guid}")] public Task<SupportArticleAdminDto> UpdateFaq(Guid id, SaveSupportArticleDto dto, CancellationToken ct) => service.SaveFaqAsync(id, dto, ct);
    [HttpDelete("faq/{id:guid}")] public async Task<IActionResult> DeleteFaq(Guid id, CancellationToken ct) { await service.DeleteFaqAsync(id, ct); return NoContent(); }

    [HttpPost("dispatches")] public Task<ContentDispatchDto> CreateDispatch(SaveContentDispatchDto dto, CancellationToken ct) => service.CreateDispatchAsync(dto, ct);
    [HttpPost("dispatches/{id:guid}/send")] public Task<ContentDispatchDto> Send(Guid id, CancellationToken ct) => service.SendAsync(id, ct);
    [HttpPost("dispatches/process-due")] public async Task<IActionResult> ProcessDue(CancellationToken ct) => Ok(new { count = await service.ProcessDueAsync(ct) });
}
