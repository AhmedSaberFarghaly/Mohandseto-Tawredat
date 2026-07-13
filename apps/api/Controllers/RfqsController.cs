using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Rfq;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/rfqs")]
public sealed class RfqsController(RfqService rfqs) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<List<RfqListDto>> List([FromQuery] string? status, CancellationToken ct) => rfqs.ListAsync(UserId, status, ct);
    [HttpPost] public Task<RfqDetailDto> Create(CreateRfqDto dto, CancellationToken ct) => rfqs.CreateAsync(UserId, dto, ct);
    [HttpGet("{id:guid}")] public Task<RfqDetailDto> Detail(Guid id, CancellationToken ct) => rfqs.DetailAsync(UserId, id, ct);
    [HttpPost("{id:guid}/items")] public Task<RfqDetailDto> AddItem(Guid id, UpsertRfqItemDto dto, CancellationToken ct) => rfqs.AddItemAsync(UserId, id, dto, ct);
    [HttpPut("{id:guid}/items/{itemId:guid}")] public Task<RfqDetailDto> UpdateItem(Guid id, Guid itemId, UpsertRfqItemDto dto, CancellationToken ct) => rfqs.UpdateItemAsync(UserId, id, itemId, dto, ct);
    [HttpDelete("{id:guid}/items/{itemId:guid}")] public Task<RfqDetailDto> RemoveItem(Guid id, Guid itemId, CancellationToken ct) => rfqs.RemoveItemAsync(UserId, id, itemId, ct);
    [HttpPost("{id:guid}/attachments"), RequestSizeLimit(15 * 1024 * 1024 + 1024)] public Task<RfqAttachmentDto> Upload(Guid id, IFormFile file, CancellationToken ct) => rfqs.UploadAsync(UserId, id, file, ct);
    [HttpGet("attachments/{id:guid}")] public async Task<IActionResult> Attachment(Guid id, CancellationToken ct) { var file = await rfqs.FileAsync(UserId, id, ct); return PhysicalFile(file.Path, file.Type, file.Name, enableRangeProcessing: true); }
    [HttpPost("{id:guid}/submit")] public Task<RfqDetailDto> Submit(Guid id, CancellationToken ct) => rfqs.SubmitAsync(UserId, id, ct);
    [HttpPost("{id:guid}/negotiate")] public Task<RfqDetailDto> Negotiate(Guid id, NegotiationRequestDto dto, CancellationToken ct) => rfqs.NegotiateAsync(UserId, id, dto, ct);
    [HttpPost("{id:guid}/quotes/{decision}")] public Task<RfqDetailDto> QuoteDecision(Guid id, string decision, QuoteDecisionDto dto, CancellationToken ct) => rfqs.QuoteDecisionAsync(UserId, id, decision, dto, ct);
    [HttpPost("{id:guid}/convert")] public Task<ConvertedRfqOrderDto> Convert(Guid id, ConvertRfqDto dto, CancellationToken ct) => rfqs.ConvertAsync(UserId, id, dto, ct);
    [HttpGet("conversion-options")] public Task<RfqConversionOptionsDto> ConversionOptions(CancellationToken ct) => rfqs.ConversionOptionsAsync(ct);
    [HttpGet("{id:guid}/quotes/{versionId:guid}/pdf")] public async Task<IActionResult> Pdf(Guid id, Guid versionId, CancellationToken ct) => File(await rfqs.QuotePdfAsync(UserId, id, versionId, ct), "application/pdf", $"quote-{id:N}.pdf");
}

[ApiController, Authorize(Roles = "super_admin,platform_admin,quotes_officer,sales_manager,sales_agent,operations_manager"), Route("api/admin/rfqs")]
public sealed class AdminRfqsController(RfqService rfqs) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpPost("{id:guid}/quotes")] public Task<RfqDetailDto> Publish(Guid id, PublishQuoteDto dto, CancellationToken ct) => rfqs.PublishQuoteAsync(id, dto, UserId, ct);
}
