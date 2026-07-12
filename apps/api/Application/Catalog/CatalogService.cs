using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Catalog;

public sealed class CatalogService(AppDbContext db, ITenantProvider tenantProvider)
{
    public async Task<IReadOnlyList<CategoryDto>> CategoriesAsync(CancellationToken ct = default)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.NameAr)
            .ToListAsync(ct);
        var counts = await db.Products.AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active)
            .GroupBy(p => p.CategoryId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        CategoryDto Map(Category c) => new(
            c.Id, c.ParentId, c.NameAr, c.NameEn, c.Slug, c.IconName, c.ImagePath,
            counts.GetValueOrDefault(c.Id),
            categories.Where(x => x.ParentId == c.Id).Select(Map).ToList());
        return categories.Where(c => c.ParentId == null).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<BrandDto>> BrandsAsync(CancellationToken ct = default) =>
        await db.Brands.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.NameAr)
            .Select(b => new BrandDto(b.Id, b.NameAr, b.NameEn, b.Slug, b.LogoPath))
            .ToListAsync(ct);

    public async Task<PagedResult<ProductCardDto>> ProductsAsync(ProductQuery request, Guid? userId, CancellationToken ct = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = db.Products.AsNoTracking()
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Unit)
            .Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.Trim();
            query = query.Where(p => EF.Functions.Like(p.NameAr, $"%{q}%") ||
                                     EF.Functions.Like(p.NameEn, $"%{q}%") ||
                                     EF.Functions.Like(p.Sku, $"%{q}%"));
        }
        if (request.CategoryId is { } categoryId)
        {
            var categoryIds = await db.Categories.Where(c => c.Id == categoryId || c.ParentId == categoryId)
                .Select(c => c.Id).ToListAsync(ct);
            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }
        if (request.BrandId is { } brandId) query = query.Where(p => p.BrandId == brandId);
        if (request.MinPrice is { } min) query = query.Where(p => p.BasePrice >= min);
        if (request.MaxPrice is { } max) query = query.Where(p => p.BasePrice <= max);
        if (request.Featured is { } featured) query = query.Where(p => p.IsFeatured == featured);
        if (request.Printable is { } printable) query = query.Where(p => p.IsPrintable == printable);
        query = request.Stock?.ToLowerInvariant() switch
        {
            "in" => query.Where(p => p.StockQty > p.LowStockThreshold),
            "low" => query.Where(p => p.StockQty > 0 && p.StockQty <= p.LowStockThreshold),
            "out" => query.Where(p => p.StockQty <= 0),
            _ => query,
        };
        query = request.Sort.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            "rating" => query.OrderByDescending(p => p.RatingAvg),
            "name" => query.OrderBy(p => p.NameAr),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.NameAr),
        };

        if (userId is { } searchUser && !string.IsNullOrWhiteSpace(request.Q))
            await RecordSearchAsync(searchUser, request.Q.Trim(), ct);

        var total = await query.CountAsync(ct);
        var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = products.Select(p => p.Id).ToList();
        var favorites = userId is null
            ? []
            : await db.Favorites.AsNoTracking().Where(f => f.UserId == userId && ids.Contains(f.ProductId))
                .Select(f => f.ProductId).ToListAsync(ct);
        var prices = await ContractPricesAsync(ids, ct);
        return new(products.Select(p => MapCard(p, ContractPrice(prices, p.Id), favorites.Contains(p.Id))).ToList(), page, pageSize, total);
    }

    public async Task<ProductDetailDto> ProductAsync(string idOrSlug, Guid? userId, CancellationToken ct = default)
    {
        var isId = Guid.TryParse(idOrSlug, out var id);
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Unit)
            .Include(p => p.Images).Include(p => p.PriceTiers).Include(p => p.Attributes)
            .Include(p => p.Variants).Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Status == ProductStatus.Active && (isId ? p.Id == id : p.Slug == idOrSlug), ct)
            ?? throw ApiException.NotFound("المنتج غير موجود");

        var price = ContractPrice(await ContractPricesAsync([product.Id], ct), product.Id);
        var isFavorite = userId is not null && await db.Favorites.AnyAsync(f => f.UserId == userId && f.ProductId == product.Id, ct);
        if (userId is { } uid && tenantProvider.TenantId is { } tid)
        {
            var recent = await db.RecentlyVieweds.FirstOrDefaultAsync(r => r.UserId == uid && r.ProductId == product.Id, ct);
            if (recent is null) db.RecentlyVieweds.Add(new RecentlyViewed { TenantId = tid, UserId = uid, ProductId = product.Id });
            else recent.ViewedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var related = await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand)
            .Include(p => p.Unit).Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Active && p.CategoryId == product.CategoryId && p.Id != product.Id)
            .OrderByDescending(p => p.IsFeatured).Take(8).ToListAsync(ct);
        var relatedPrices = await ContractPricesAsync(related.Select(p => p.Id).ToList(), ct);
        return new(
            MapCard(product, price, isFavorite), product.DescriptionAr, product.DescriptionEn,
            product.TaxRatePercent, product.MaxOrderQty, product.WarrantyAr, product.DeliveryEstimateDays,
            product.Images.OrderBy(i => i.SortOrder).Select(i => new ProductImageDto(i.Id, MediaUrl("images", i.Id, i.Path), i.AltAr, i.IsPrimary, i.SortOrder)).ToList(),
            product.PriceTiers.OrderBy(t => t.MinQty).Select(t => new PriceTierDto(t.MinQty, t.UnitPrice)).ToList(),
            product.Attributes.OrderBy(a => a.SortOrder).Select(a => new AttributeDto(a.NameAr, a.ValueAr, a.SortOrder)).ToList(),
            product.Variants.OrderBy(v => v.NameAr).Select(v => new VariantDto(v.Id, v.Sku, v.NameAr, v.NameEn, v.OptionsJson, (price ?? product.BasePrice) + v.PriceAdjustment, v.StockQty, v.IsActive)).ToList(),
            product.Documents.Select(d => new ProductDocumentDto(d.Id, d.NameAr, MediaUrl("documents", d.Id, d.Path), d.ContentType)).ToList(),
            related.Select(p => MapCard(p, ContractPrice(relatedPrices, p.Id), false)).ToList());
    }

    public async Task<IReadOnlyList<string>> SuggestionsAsync(string q, CancellationToken ct = default)
    {
        q = q.Trim();
        if (q.Length < 2) return [];
        return await db.Products.AsNoTracking().Where(p => p.Status == ProductStatus.Active &&
                (EF.Functions.Like(p.NameAr, $"%{q}%") || EF.Functions.Like(p.NameEn, $"%{q}%") || EF.Functions.Like(p.Sku, $"%{q}%")))
            .OrderByDescending(p => p.IsFeatured).Select(p => p.NameAr).Distinct().Take(10).ToListAsync(ct);
    }

    public async Task<bool> ToggleFavoriteAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.Status == ProductStatus.Active, ct))
            throw ApiException.NotFound("المنتج غير موجود");
        var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId, ct);
        if (favorite is null) db.Favorites.Add(new Favorite { TenantId = tenantId, UserId = userId, ProductId = productId });
        else db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(ct);
        return favorite is null;
    }

    public async Task<bool> ToggleCompareAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
        var item = await db.CompareItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId, ct);
        if (item is null)
        {
            if (await db.CompareItems.CountAsync(c => c.UserId == userId, ct) >= 4)
                throw ApiException.BadRequest("يمكنك مقارنة أربعة منتجات بحد أقصى");
            db.CompareItems.Add(new CompareItem { TenantId = tenantId, UserId = userId, ProductId = productId });
        }
        else db.CompareItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return item is null;
    }

    public async Task<IReadOnlyList<ProductCardDto>> FavoritesAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await db.Favorites.AsNoTracking().Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt).Select(f => f.ProductId).ToListAsync(ct);
        if (ids.Count == 0) return [];
        var products = await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand)
            .Include(p => p.Unit).Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Active && ids.Contains(p.Id)).ToListAsync(ct);
        var prices = await ContractPricesAsync(ids, ct);
        return products.OrderBy(p => ids.IndexOf(p.Id))
            .Select(p => MapCard(p, ContractPrice(prices, p.Id), true)).ToList();
    }

    public async Task<IReadOnlyList<ProductCardDto>> RecentlyViewedAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await db.RecentlyVieweds.AsNoTracking().Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ViewedAt).Select(r => r.ProductId).Take(20).ToListAsync(ct);
        if (ids.Count == 0) return [];
        var products = await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand)
            .Include(p => p.Unit).Include(p => p.Images)
            .Where(p => p.Status == ProductStatus.Active && ids.Contains(p.Id)).ToListAsync(ct);
        var prices = await ContractPricesAsync(ids, ct);
        return products.OrderBy(p => ids.IndexOf(p.Id))
            .Select(p => MapCard(p, ContractPrice(prices, p.Id), false)).ToList();
    }

    public async Task<IReadOnlyList<string>> RecentSearchesAsync(Guid userId, CancellationToken ct = default) =>
        await db.RecentSearches.AsNoTracking().Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SearchedAt).Select(s => s.Query).Take(10).ToListAsync(ct);

    public async Task ClearRecentSearchesAsync(Guid userId, CancellationToken ct = default)
    {
        var searches = await db.RecentSearches.Where(s => s.UserId == userId).ToListAsync(ct);
        db.RecentSearches.RemoveRange(searches);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<CompareProductDto>> CompareAsync(Guid userId, CancellationToken ct = default)
    {
        var ids = await db.CompareItems.AsNoTracking().Where(c => c.UserId == userId)
            .OrderBy(c => c.CreatedAt).Select(c => c.ProductId).ToListAsync(ct);
        if (ids.Count == 0) return [];
        var products = await db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand)
            .Include(p => p.Unit).Include(p => p.Images).Include(p => p.Attributes)
            .Where(p => p.Status == ProductStatus.Active && ids.Contains(p.Id)).ToListAsync(ct);
        var prices = await ContractPricesAsync(ids, ct);
        return products.OrderBy(p => ids.IndexOf(p.Id)).Select(p => new CompareProductDto(
            MapCard(p, ContractPrice(prices, p.Id), false),
            p.Attributes.OrderBy(a => a.SortOrder).ToDictionary(a => a.NameAr, a => a.ValueAr))).ToList();
    }

    public async Task ClearCompareAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await db.CompareItems.Where(c => c.UserId == userId).ToListAsync(ct);
        db.CompareItems.RemoveRange(items);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Dictionary<Guid, decimal>> ContractPricesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        if (tenantProvider.TenantId is null || productIds.Count == 0) return [];
        var now = DateTime.UtcNow;
        return await db.CompanyProductPrices.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId) && (p.ValidFrom == null || p.ValidFrom <= now) && (p.ValidTo == null || p.ValidTo >= now))
            .GroupBy(p => p.ProductId)
            .Select(g => g.OrderByDescending(x => x.ValidFrom).First())
            .ToDictionaryAsync(p => p.ProductId, p => p.ContractPrice, ct);
    }

    private async Task RecordSearchAsync(Guid userId, string query, CancellationToken ct)
    {
        if (tenantProvider.TenantId is not { } tenantId || query.Length < 2) return;
        query = query.Length > 100 ? query[..100] : query;
        var existing = await db.RecentSearches.FirstOrDefaultAsync(s => s.UserId == userId && s.Query == query, ct);
        if (existing is null)
            db.RecentSearches.Add(new RecentSearch { TenantId = tenantId, UserId = userId, Query = query });
        else existing.SearchedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static ProductCardDto MapCard(Product p, decimal? contractPrice, bool isFavorite)
    {
        var primary = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault();
        return new(p.Id, p.Sku, p.NameAr, p.NameEn, p.Slug, p.Category.NameAr, p.Brand?.NameAr,
            contractPrice ?? p.BasePrice, p.CompareAtPrice, contractPrice is not null, p.GetStockStatus().ToString(),
            p.StockQty, primary is null ? null : MediaUrl("images", primary.Id, primary.Path), p.RatingAvg, p.RatingCount, p.MinOrderQty, p.Unit.NameAr,
            p.IsPrintable, p.IsFeatured, isFavorite);
    }

    private static decimal? ContractPrice(IReadOnlyDictionary<Guid, decimal> prices, Guid productId) =>
        prices.TryGetValue(productId, out var price) ? price : null;

    private static string MediaUrl(string kind, Guid id, string path) =>
        path.StartsWith("storage/catalog/", StringComparison.OrdinalIgnoreCase) ? $"/api/catalog/media/{kind}/{id}" : path;
}
