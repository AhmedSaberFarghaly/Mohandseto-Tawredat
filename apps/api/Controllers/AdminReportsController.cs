using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminReports;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/reports"), Authorize(Roles = "super_admin,system_admin,sales_manager,operations_manager,accountant,auditor")]
public sealed class AdminReportsController(AdminReportService reports) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id
        : throw ApiException.Unauthorized();

    [HttpGet]
    public Task<ReportsDashboardDto> Dashboard(CancellationToken ct) => reports.DashboardAsync(ct);

    [HttpPost("built-in/{code}")]
    public Task<ReportResultDto> BuiltIn(string code, ReportFilterDto filter, CancellationToken ct) => reports.BuiltInAsync(code, filter, ct);

    [HttpPost("preview")]
    public Task<ReportResultDto> Preview(PreviewCustomReportDto dto, CancellationToken ct) => reports.PreviewAsync(dto, ct);

    [HttpPost("templates"), Authorize(Roles = "super_admin,system_admin,sales_manager,operations_manager,accountant")]
    public Task<SavedReportDto> Save(SaveCustomReportDto dto, CancellationToken ct) => reports.SaveAsync(UserId, dto, ct);

    [HttpPut("templates/{id:guid}/schedule"), Authorize(Roles = "super_admin,system_admin,sales_manager,operations_manager,accountant")]
    public Task<SavedReportDto> Schedule(Guid id, ScheduleReportDto dto, CancellationToken ct) => reports.ScheduleAsync(UserId, id, dto, ct);

    [HttpDelete("templates/{id:guid}"), Authorize(Roles = "super_admin,system_admin,sales_manager,operations_manager,accountant")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await reports.DeleteAsync(UserId, id, ct);
        return NoContent();
    }

    [HttpPost("export/excel")]
    public async Task<IActionResult> Excel(ReportExportOptionsDto dto, CancellationToken ct)
        => File(await reports.ExportExcelAsync(dto, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"report-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");

    [HttpPost("export/pdf")]
    public async Task<IActionResult> Pdf(ReportExportOptionsDto dto, CancellationToken ct)
        => File(await reports.ExportPdfAsync(dto, ct), "application/pdf", $"report-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");

    [HttpPost("process-due"), Authorize(Roles = "super_admin,system_admin")]
    public async Task<IActionResult> ProcessDue(CancellationToken ct) => Ok(new { processed = await reports.ProcessDueAsync(ct) });
}
