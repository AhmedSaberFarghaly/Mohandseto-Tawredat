using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Catalog;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    private Guid? UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : null;

    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) => catalog.CategoriesAsync(ct);

    [HttpGet("brands")]
    public Task<IReadOnlyList<BrandDto>> Brands(CancellationToken ct) => catalog.BrandsAsync(ct);

    [HttpGet("products")]
    public Task<PagedResult<ProductCardDto>> Products([FromQuery] ProductQuery query, CancellationToken ct) =>
        catalog.ProductsAsync(query, UserId, ct);

    [HttpGet("products/{idOrSlug}")]
    public Task<ProductDetailDto> Product(string idOrSlug, CancellationToken ct) =>
        catalog.ProductAsync(idOrSlug, UserId, ct);

    [HttpGet("search/suggestions")]
    public Task<IReadOnlyList<string>> Suggestions([FromQuery] string q, CancellationToken ct) =>
        catalog.SuggestionsAsync(q, ct);

    [Authorize]
    [HttpPost("favorites/{productId:guid}/toggle")]
    public async Task<IActionResult> ToggleFavorite(Guid productId, CancellationToken ct) =>
        Ok(new { isFavorite = await catalog.ToggleFavoriteAsync(productId, UserId!.Value, ct) });

    [Authorize]
    [HttpGet("favorites")]
    public Task<IReadOnlyList<ProductCardDto>> Favorites(CancellationToken ct) =>
        catalog.FavoritesAsync(UserId!.Value, ct);

    [Authorize]
    [HttpGet("recently-viewed")]
    public Task<IReadOnlyList<ProductCardDto>> RecentlyViewed(CancellationToken ct) =>
        catalog.RecentlyViewedAsync(UserId!.Value, ct);

    [Authorize]
    [HttpPost("compare/{productId:guid}/toggle")]
    public async Task<IActionResult> ToggleCompare(Guid productId, CancellationToken ct) =>
        Ok(new { isCompared = await catalog.ToggleCompareAsync(productId, UserId!.Value, ct) });
}
