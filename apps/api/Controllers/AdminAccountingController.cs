using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminAccounting;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Finance;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize(Roles = "super_admin,accountant,auditor"), Route("api/admin/accounting")]
public sealed class AdminAccountingController(AdminAccountingService accounting, FinanceService finance) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<AccountingDashboardDto> Dashboard(CancellationToken ct) => accounting.DashboardAsync(ct);
    [HttpPost("invoices")] public Task<AccountingInvoiceDto> Invoice(CreateAdminInvoiceDto dto, CancellationToken ct) => accounting.CreateInvoiceAsync(dto, ct);
    [HttpGet("invoices/{id:guid}/pdf")] public async Task<IActionResult> Pdf(Guid id, CancellationToken ct) => File(await finance.PdfAsync(id, ct), "application/pdf", $"invoice-{id:N}.pdf");
    [HttpGet("export")] public async Task<IActionResult> Export([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => File(await finance.ExportAsync(null, from, to, ct), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "accounting-export.xlsx");
    [HttpPost("payments/bank-transfer")] public Task<AccountingPaymentDto> Transfer(RecordBankTransferDto dto, CancellationToken ct) => accounting.RecordTransferAsync(dto, ct);
    [HttpPost("payments/{id:guid}/decision")] public Task<InvoicePaymentDto> Match(Guid id, PaymentDecisionDto dto, CancellationToken ct) => finance.DecidePaymentAsync(UserId, id, dto, ct);
    [HttpPost("entries")] public Task<AccountingEntryDto> Entry(SaveAccountingEntryDto dto, CancellationToken ct) => accounting.CreateEntryAsync(UserId, dto, ct);
    [HttpPost("entries/{id:guid}/post")] public async Task<IActionResult> Post(Guid id, CancellationToken ct) { await accounting.PostEntryAsync(UserId, id, ct); return NoContent(); }
    [HttpPost("periods/close")] public Task<FinancialPeriodDto> Close(CloseFinancialPeriodDto dto, CancellationToken ct) => accounting.ClosePeriodAsync(UserId, dto, ct);
}
