using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminQuotes;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Rfq;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize(Roles = "super_admin,platform_admin,quotes_officer,sales_manager,sales_agent,operations_manager,auditor"), Route("api/admin/quotes")]
public sealed class AdminQuotesController(AdminQuoteService quotes) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<AdminQuotePageDto> List([FromQuery] string? search, [FromQuery] string? statuses, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) => quotes.ListAsync(search, statuses, page, pageSize, ct);
    [HttpGet("{id:guid}")] public Task<AdminQuoteDetailDto> Detail(Guid id, CancellationToken ct) => quotes.DetailAsync(id, ct);
    [HttpPost("{id:guid}/items/{itemId:guid}/review")] public Task<AdminQuoteDetailDto> Review(Guid id, Guid itemId, ReviewAdminRfqItemDto dto, CancellationToken ct) => quotes.ReviewItemAsync(id, itemId, dto, ct);
    [HttpPost("{id:guid}/items/{itemId:guid}/link")] public Task<AdminQuoteDetailDto> Link(Guid id, Guid itemId, LinkAdminRfqProductDto dto, CancellationToken ct) => quotes.LinkProductAsync(id, itemId, dto.ProductId, ct);
    [HttpPost("{id:guid}/items/{itemId:guid}/temporary")] public Task<AdminQuoteDetailDto> Temporary(Guid id, Guid itemId, CreateTemporaryRfqProductDto dto, CancellationToken ct) => quotes.CreateTemporaryAsync(id, itemId, dto, ct);
    [HttpPost("{id:guid}/supplier-requests")] public Task<AdminQuoteDetailDto> RequestSupplier(Guid id, RequestSupplierPriceDto dto, CancellationToken ct) => quotes.RequestSupplierAsync(id, dto, ct);
    [HttpPost("{id:guid}/supplier-quotes")] public Task<AdminQuoteDetailDto> SupplierQuote(Guid id, RecordSupplierPriceDto dto, CancellationToken ct) => quotes.RecordSupplierPriceAsync(id, dto, ct);
    [HttpPost("{id:guid}/versions")] public Task<AdminQuoteDetailDto> SaveQuote(Guid id, SaveCustomerQuoteDto dto, CancellationToken ct) => quotes.SaveQuoteAsync(id, dto, ct);
    [HttpPost("{id:guid}/versions/{versionId:guid}/send")] public Task<AdminQuoteDetailDto> Send(Guid id, Guid versionId, CancellationToken ct) => quotes.SendAsync(id, versionId, ct);
    [HttpPost("{id:guid}/versions/{versionId:guid}/accept")] public Task<AdminQuoteDetailDto> Accept(Guid id, Guid versionId, CancellationToken ct) => quotes.AcceptAsync(id, versionId, ct);
    [HttpPost("{id:guid}/negotiate")] public Task<AdminQuoteDetailDto> Negotiate(Guid id, StaffNegotiationDto dto, CancellationToken ct) => quotes.NegotiateAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/assign")] public Task<AdminQuoteDetailDto> Assign(Guid id, AssignQuoteDto dto, CancellationToken ct) => quotes.AssignAsync(id, dto.StaffUserId, ct);
    [HttpPost("{id:guid}/convert")] public Task<ConvertedRfqOrderDto> Convert(Guid id, ConvertRfqDto dto, CancellationToken ct) => quotes.ConvertAsync(id, dto, ct);
    [HttpGet("{id:guid}/conversion-options")] public Task<RfqConversionOptionsDto> ConversionOptions(Guid id, CancellationToken ct) => quotes.ConversionOptionsAsync(id, ct);
    [HttpGet("{id:guid}/versions/{versionId:guid}/pdf")] public async Task<IActionResult> Pdf(Guid id, Guid versionId, CancellationToken ct) => File(await quotes.PdfAsync(id, versionId, ct), "application/pdf", $"quote-{id:N}.pdf");
    [HttpGet("products")] public Task<List<AdminProductOptionDto>> Products([FromQuery] string? search, CancellationToken ct) => quotes.ProductsAsync(search, ct);
    [HttpGet("templates")] public Task<List<AdminQuoteTemplateDto>> Templates(CancellationToken ct) => quotes.TemplatesAsync(ct);
    [HttpPost("templates")] public Task<AdminQuoteTemplateDto> CreateTemplate(SaveQuoteTemplateDto dto, CancellationToken ct) => quotes.SaveTemplateAsync(null, dto, ct);
    [HttpPut("templates/{id:guid}")] public Task<AdminQuoteTemplateDto> UpdateTemplate(Guid id, SaveQuoteTemplateDto dto, CancellationToken ct) => quotes.SaveTemplateAsync(id, dto, ct);
}
