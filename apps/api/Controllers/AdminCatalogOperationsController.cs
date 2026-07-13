using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/admin/catalog"), Authorize(Roles = "super_admin,products_manager,system_admin,sales_manager")]
public sealed class AdminCatalogOperationsController(AdminCatalogOperationsService operations) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet("products/{id:guid}/commercial")] public Task<ProductCommercialDto> Detail(Guid id, CancellationToken ct) => operations.DetailAsync(id, ct);
    [HttpPut("products/{id:guid}/commercial")] public Task<ProductCommercialDto> Save(Guid id, SaveProductCommercialDto dto, CancellationToken ct) => operations.SaveAsync(id, dto, ct);
    [HttpPut("products/{id:guid}/links")] public Task<ProductCommercialDto> Links(Guid id, ReplaceProductLinksDto dto, CancellationToken ct) => operations.ReplaceLinksAsync(id, dto, ct);
    [HttpPut("products/{id:guid}/company-prices")] public Task<ProductCommercialDto> CompanyPrices(Guid id, ReplaceCompanyPricesDto dto, CancellationToken ct) => operations.ReplaceCompanyPricesAsync(id, dto, ct);
    [HttpPost("products/bulk-prices")] public Task<List<ProductPriceChangeDto>> BulkPrices(BulkPriceUpdateDto dto, CancellationToken ct) => operations.BulkPricesAsync(UserId, dto, ct);
    [HttpGet("products/price-history")] public Task<List<ProductPriceChangeDto>> PriceHistory([FromQuery] Guid? productId, [FromQuery] int limit = 200, CancellationToken ct = default) => operations.HistoryAsync(productId is null ? null : [productId.Value], limit, ct);
    [HttpPost("products/{id:guid}/status")] public async Task<IActionResult> Status(Guid id, SetProductStatusDto dto, CancellationToken ct) { await operations.SetStatusAsync(id, dto.Status, ct); return NoContent(); }
}
