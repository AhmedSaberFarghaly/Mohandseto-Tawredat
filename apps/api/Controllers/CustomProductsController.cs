using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Application.Shopping;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/custom-products")]
public sealed class CustomProductsController(CustomizationService customization, CartService cart) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw ApiException.Unauthorized();

    [HttpGet("templates")]
    public Task<IReadOnlyList<CustomTemplateSummaryDto>> Templates(CancellationToken ct) => customization.TemplatesAsync(ct);

    [HttpGet("logos")]
    public Task<IReadOnlyList<SavedLogoDto>> SavedLogos(CancellationToken ct) => customization.SavedLogosAsync(ct);

    [HttpGet("templates/{id:guid}")]
    public Task<CustomTemplateDto> Template(Guid id, CancellationToken ct) => customization.TemplateAsync(id, ct);

    [HttpGet("requests")]
    public Task<IReadOnlyList<CustomRequestListDto>> Requests(CancellationToken ct) => customization.RequestsAsync(UserId, ct);

    [HttpGet("requests/{id:guid}")]
    public Task<CustomRequestDto> GetRequest(Guid id, CancellationToken ct) => customization.RequestAsync(UserId, id, ct);

    [HttpPost("requests"), RequestSizeLimit(32 * 1024 * 1024)]
    public Task<CustomRequestDto> Create([FromForm] CreateCustomRequestForm form, CancellationToken ct) => customization.CreateAsync(UserId, form, ct);

    [HttpPut("requests/{id:guid}/design-brief")]
    public Task<CustomRequestDto> SaveBrief(Guid id, SaveDesignBriefDto dto, CancellationToken ct) => customization.SaveBriefAsync(UserId, id, dto, ct);

    [HttpPost("requests/{id:guid}/comments")]
    public Task<CustomRequestDto> AddComment(Guid id, AddDesignCommentDto dto, CancellationToken ct) => customization.AddCommentAsync(UserId, id, dto, ct);

    [HttpPost("requests/{id:guid}/quote-response")]
    public Task<CustomRequestDto> QuoteResponse(Guid id, QuoteResponseDto dto, CancellationToken ct) => customization.RespondToQuoteAsync(UserId, id, dto.Accept, ct);

    [HttpPost("requests/{id:guid}/design-decision")]
    public Task<CustomRequestDto> DesignDecision(Guid id, DesignDecisionDto dto, CancellationToken ct) => customization.DesignDecisionAsync(UserId, id, dto, ct);

    [HttpPost("requests/{id:guid}/add-to-cart")]
    public Task<CartDto> AddToCart(Guid id, CancellationToken ct) => cart.AddCustomRequestAsync(UserId, id, ct);

    [HttpPost("requests/{requestId:guid}/samples/{sampleId:guid}/decision")]
    public Task<CustomRequestDto> SampleDecision(Guid requestId, Guid sampleId, SampleDecisionDto dto, CancellationToken ct) =>
        customization.SampleDecisionAsync(UserId, requestId, sampleId, dto, ct);

    [HttpGet("files/{id:guid}")]
    public async Task<IActionResult> FileAsset(Guid id, CancellationToken ct)
    {
        var file = await customization.AssetAsync(id, ct);
        return PhysicalFile(file.Path, file.ContentType, file.Name, enableRangeProcessing: true);
    }

    [HttpGet("mockups/{id:guid}")]
    public async Task<IActionResult> Mockup(Guid id, CancellationToken ct)
    {
        var file = await customization.MockupAsync(id, ct);
        return PhysicalFile(file.Path, file.ContentType, file.Name, enableRangeProcessing: true);
    }

    [HttpGet("samples/{id:guid}")]
    public async Task<IActionResult> Sample(Guid id, CancellationToken ct)
    {
        var file = await customization.SampleFileAsync(id, ct);
        return PhysicalFile(file.Path, file.ContentType, file.Name, enableRangeProcessing: true);
    }
}

[ApiController, Authorize(Roles = "super_admin,graphic_designer,printing_officer,operations_manager"), Route("api/admin/custom-products")]
public sealed class AdminCustomProductsController(CustomizationService customization) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw ApiException.Unauthorized();

    [HttpGet("requests/{id:guid}")]
    public Task<CustomRequestDto> GetRequest(Guid id, CancellationToken ct) => customization.AdminRequestAsync(id, ct);

    [HttpPut("requests/{id:guid}/quote")]
    public Task<CustomRequestDto> SetQuote(Guid id, SetCustomQuoteDto dto, CancellationToken ct) => customization.SetQuoteAsync(id, dto, ct);

    [HttpPost("requests/{id:guid}/design-versions"), RequestSizeLimit(24 * 1024 * 1024)]
    public Task<CustomRequestDto> PublishDesign(Guid id, [FromForm] PublishDesignForm form, CancellationToken ct) => customization.PublishDesignAsync(id, UserId, form, ct);

    [HttpPost("requests/{id:guid}/samples"), RequestSizeLimit(24 * 1024 * 1024)]
    public Task<CustomRequestDto> PublishSample(Guid id, [FromForm] PublishSampleForm form, CancellationToken ct) =>
        customization.PublishSampleAsync(id, form, ct);

    [HttpPut("requests/{requestId:guid}/production-stages/{stageId:guid}")]
    public Task<CustomRequestDto> UpdateStage(Guid requestId, Guid stageId, UpdateProductionStageDto dto, CancellationToken ct) =>
        customization.UpdateStageAsync(requestId, stageId, dto, ct);

    [HttpPost("requests/{id:guid}/quality-checks")]
    public Task<CustomRequestDto> AddQualityCheck(Guid id, AddQualityCheckDto dto, CancellationToken ct) =>
        customization.AddQualityCheckAsync(id, UserId, dto, ct);
}
