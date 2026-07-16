using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Finance;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/finance")]
public sealed class FinanceController(FinanceService finance) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet("invoices")] public Task<List<InvoiceListDto>> Invoices([FromQuery] string? status, [FromQuery] string? search, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => finance.ListAsync(status, search, from, to, ct);
    [HttpGet("invoices/{id:guid}")] public Task<InvoiceDetailDto> Invoice(Guid id, CancellationToken ct) => finance.DetailAsync(id, ct);
    [HttpGet("invoices/{id:guid}/pdf")] public async Task<IActionResult> Pdf(Guid id, CancellationToken ct) => File(await finance.PdfAsync(id, ct), "application/pdf", $"invoice-{id:N}.pdf");
    [HttpGet("invoices/export")]
    public async Task<IActionResult> Export([FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? format, CancellationToken ct)
    {
        var export = await finance.ExportFileAsync(status, from, to, format, ct);
        return File(export.Content, export.ContentType, export.FileName);
    }
    [HttpGet("summary")] public Task<FinanceSummaryDto> Summary(CancellationToken ct) => finance.SummaryAsync(ct);
    [HttpPost("invoices/{id:guid}/payments")] public Task<InvoicePaymentStartedDto> StartPayment(Guid id, StartInvoicePaymentDto dto, CancellationToken ct) => finance.StartPaymentAsync(UserId, id, dto, ct);
    [HttpPost("payments/{id:guid}/receipt"), RequestSizeLimit(10 * 1024 * 1024 + 4096)] public Task<InvoicePaymentDto> Receipt(Guid id, IFormFile file, CancellationToken ct) => finance.UploadReceiptAsync(UserId, id, file, ct);
    [HttpPost("credit-limit-requests")] public Task<CreditLimitRequestResultDto> Credit(CreditLimitRequestDto dto, CancellationToken ct) => finance.RequestCreditAsync(UserId, dto, ct);
}

[ApiController, Authorize(Roles = "super_admin,accountant,auditor"), Route("api/admin/finance")]
public sealed class AdminFinanceController(FinanceService finance) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpPost("payments/{id:guid}/decision")] public Task<InvoicePaymentDto> Payment(Guid id, PaymentDecisionDto dto, CancellationToken ct) => finance.DecidePaymentAsync(UserId, id, dto, ct);
    [HttpPost("credit-limit-requests/{id:guid}/decision")] public Task<CreditLimitRequestResultDto> Credit(Guid id, CreditDecisionDto dto, CancellationToken ct) => finance.DecideCreditAsync(UserId, id, dto, ct);
}
