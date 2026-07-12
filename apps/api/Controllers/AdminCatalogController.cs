using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Catalog;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Route("api/admin/catalog")]
[Authorize(Roles = "super_admin,products_manager,system_admin")]
public sealed class AdminCatalogController(AppDbContext db, CatalogService catalog) : ControllerBase
{
    [HttpGet("products")]
    public Task<PagedResult<ProductCardDto>> Products([FromQuery] ProductQuery query, CancellationToken ct) =>
        catalog.ProductsAsync(query, null, ct);

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(UpsertProductDto dto, CancellationToken ct)
    {
        await ValidateProductAsync(dto, null, ct);
        var product = Map(new Product(), dto);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return Created($"/api/admin/catalog/products/{product.Id}", new { product.Id });
    }

    [HttpGet("products/{id:guid}")]
    public async Task<IActionResult> Product(Guid id, CancellationToken ct)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("المنتج غير موجود");
        return Ok(new
        {
            p.Id, p.Sku, p.NameAr, p.NameEn, p.Slug, p.DescriptionAr, p.DescriptionEn,
            p.CategoryId, p.BrandId, p.UnitId, p.BasePrice, p.CompareAtPrice, p.TaxRatePercent,
            p.MinOrderQty, p.MaxOrderQty, p.StockQty, p.LowStockThreshold,
            status = p.Status.ToString(), p.IsPrintable, p.IsFeatured, p.WarrantyAr, p.DeliveryEstimateDays,
        });
    }

    [HttpPut("products/{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpsertProductDto dto, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw ApiException.NotFound("المنتج غير موجود");
        await ValidateProductAsync(dto, id, ct);
        Map(product, dto);
        await db.SaveChangesAsync(ct);
        return Ok(new { product.Id });
    }

    [HttpDelete("products/{id:guid}")]
    public async Task<IActionResult> ArchiveProduct(Guid id, CancellationToken ct)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw ApiException.NotFound("المنتج غير موجود");
        product.Status = ProductStatus.Archived;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("products/{id:guid}/price-tiers")]
    public async Task<IActionResult> ReplacePriceTiers(Guid id, IReadOnlyList<PriceTierDto> tiers, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        if (tiers.Any(t => t.MinQty < 1 || t.UnitPrice <= 0) || tiers.Select(t => t.MinQty).Distinct().Count() != tiers.Count)
            throw ApiException.BadRequest("شرائح الأسعار غير صالحة");
        var existing = await db.QuantityPriceTiers.Where(t => t.ProductId == id).ToListAsync(ct);
        db.QuantityPriceTiers.RemoveRange(existing);
        db.QuantityPriceTiers.AddRange(tiers.OrderBy(t => t.MinQty)
            .Select(t => new QuantityPriceTier { ProductId = id, MinQty = t.MinQty, UnitPrice = t.UnitPrice }));
        await db.SaveChangesAsync(ct);
        return Ok(tiers.OrderBy(t => t.MinQty));
    }

    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> Categories(CancellationToken ct) => catalog.CategoriesAsync(ct);

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(UpsertCategoryDto dto, CancellationToken ct)
    {
        await ValidateCategoryAsync(dto, null, ct);
        var category = Map(new Category(), dto);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return Created($"/api/admin/catalog/categories/{category.Id}", new { category.Id });
    }

    [HttpPut("categories/{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpsertCategoryDto dto, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiException.NotFound("القسم غير موجود");
        if (dto.ParentId == id) throw ApiException.BadRequest("لا يمكن أن يكون القسم تابعًا لنفسه");
        await ValidateCategoryAsync(dto, id, ct);
        Map(category, dto);
        await db.SaveChangesAsync(ct);
        return Ok(new { category.Id });
    }

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken ct) => Ok(new
    {
        brands = await db.Brands.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.NameAr)
            .Select(b => new { b.Id, b.NameAr, b.NameEn }).ToListAsync(ct),
        units = await db.Units.AsNoTracking().OrderBy(u => u.NameAr)
            .Select(u => new { u.Id, u.Code, u.NameAr, u.NameEn }).ToListAsync(ct),
    });

    private async Task ValidateProductAsync(UpsertProductDto dto, Guid? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NameAr) || string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Slug))
            throw ApiException.BadRequest("الاسم والرمز والرابط المختصر مطلوبة");
        if (dto.BasePrice <= 0 || dto.MinOrderQty < 1) throw ApiException.BadRequest("السعر والحد الأدنى للطلب غير صالحين");
        if (!Enum.TryParse<ProductStatus>(dto.Status, true, out _)) throw ApiException.BadRequest("حالة المنتج غير صالحة");
        if (!await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct)) throw ApiException.BadRequest("القسم غير موجود");
        if (!await db.Units.AnyAsync(u => u.Id == dto.UnitId, ct)) throw ApiException.BadRequest("وحدة البيع غير موجودة");
        if (await db.Products.AnyAsync(p => p.Id != id && (p.Sku == dto.Sku.Trim() || p.Slug == dto.Slug.Trim()), ct))
            throw ApiException.Conflict("رمز المنتج أو الرابط المختصر مستخدم بالفعل");
    }

    private async Task ValidateCategoryAsync(UpsertCategoryDto dto, Guid? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NameAr) || string.IsNullOrWhiteSpace(dto.Slug))
            throw ApiException.BadRequest("اسم القسم والرابط المختصر مطلوبان");
        if (dto.ParentId is { } parentId && !await db.Categories.AnyAsync(c => c.Id == parentId, ct))
            throw ApiException.BadRequest("القسم الرئيسي غير موجود");
        if (await db.Categories.AnyAsync(c => c.Id != id && c.Slug == dto.Slug.Trim(), ct))
            throw ApiException.Conflict("الرابط المختصر مستخدم بالفعل");
    }

    private static Product Map(Product product, UpsertProductDto dto)
    {
        product.Sku = dto.Sku.Trim(); product.NameAr = dto.NameAr.Trim(); product.NameEn = dto.NameEn.Trim();
        product.Slug = dto.Slug.Trim(); product.DescriptionAr = dto.DescriptionAr?.Trim(); product.DescriptionEn = dto.DescriptionEn?.Trim();
        product.CategoryId = dto.CategoryId; product.BrandId = dto.BrandId; product.UnitId = dto.UnitId;
        product.BasePrice = dto.BasePrice; product.CompareAtPrice = dto.CompareAtPrice; product.TaxRatePercent = dto.TaxRatePercent;
        product.MinOrderQty = dto.MinOrderQty; product.MaxOrderQty = dto.MaxOrderQty; product.StockQty = dto.StockQty;
        product.LowStockThreshold = dto.LowStockThreshold; product.Status = Enum.Parse<ProductStatus>(dto.Status, true);
        product.IsPrintable = dto.IsPrintable; product.IsFeatured = dto.IsFeatured; product.WarrantyAr = dto.WarrantyAr?.Trim();
        product.DeliveryEstimateDays = Math.Max(1, dto.DeliveryEstimateDays);
        return product;
    }

    private static Category Map(Category category, UpsertCategoryDto dto)
    {
        category.ParentId = dto.ParentId; category.NameAr = dto.NameAr.Trim(); category.NameEn = dto.NameEn.Trim();
        category.Slug = dto.Slug.Trim(); category.IconName = dto.IconName?.Trim(); category.ImagePath = dto.ImagePath?.Trim();
        category.SortOrder = dto.SortOrder; category.IsActive = dto.IsActive;
        return category;
    }
}
