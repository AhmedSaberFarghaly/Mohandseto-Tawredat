using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(CatalogService catalog, AppDbContext db, IWebHostEnvironment? env = null) : ControllerBase
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

    [Authorize]
    [HttpGet("compare")]
    public Task<IReadOnlyList<CompareProductDto>> Compare(CancellationToken ct) =>
        catalog.CompareAsync(UserId!.Value, ct);

    [Authorize]
    [HttpDelete("compare")]
    public async Task<IActionResult> ClearCompare(CancellationToken ct)
    {
        await catalog.ClearCompareAsync(UserId!.Value, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("search/recent")]
    public Task<IReadOnlyList<string>> RecentSearches(CancellationToken ct) =>
        catalog.RecentSearchesAsync(UserId!.Value, ct);

    [Authorize]
    [HttpDelete("search/recent")]
    public async Task<IActionResult> ClearRecentSearches(CancellationToken ct)
    {
        await catalog.ClearRecentSearchesAsync(UserId!.Value, ct);
        return NoContent();
    }

    [HttpGet("media/{kind}/{id:guid}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Media(string kind, Guid id, CancellationToken ct)
    {
        string? path; string contentType;
        if (kind.Equals("images", StringComparison.OrdinalIgnoreCase))
        {
            var image = await db.ProductImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct)
                ?? throw ApiException.NotFound("الصورة غير موجودة");
            path = image.Path;
            contentType = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" };
        }
        else if (kind.Equals("documents", StringComparison.OrdinalIgnoreCase))
        {
            var document = await db.ProductDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct)
                ?? throw ApiException.NotFound("المستند غير موجود");
            path = document.Path; contentType = document.ContentType;
        }
        else throw ApiException.NotFound();

        if (!path.StartsWith("storage/catalog/", StringComparison.OrdinalIgnoreCase)) throw ApiException.NotFound("الملف غير متاح");
        var root = env?.ContentRootPath ?? AppContext.BaseDirectory;
        var storageRoot = Path.GetFullPath(Path.Combine(root, "storage", "catalog"));
        var fullPath = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            throw ApiException.NotFound("الملف غير متاح");
        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }
}
