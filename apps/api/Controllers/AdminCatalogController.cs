using System.Globalization;
using System.Text;
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
public sealed class AdminCatalogController(AppDbContext db, CatalogService catalog, IWebHostEnvironment? env = null) : ControllerBase
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] ImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private const long MaxDocumentBytes = 10 * 1024 * 1024;
    private string ContentRoot => env?.ContentRootPath ?? AppContext.BaseDirectory;
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

    [HttpGet("products/export")]
    public async Task<IActionResult> ExportProducts([FromQuery] string? q, CancellationToken ct)
    {
        var query = db.Products.AsNoTracking().Include(p => p.Category).Include(p => p.Brand).Include(p => p.Unit).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Sku.Contains(term) || p.NameAr.Contains(term) || p.NameEn.Contains(term));
        }

        var products = await query.OrderBy(p => p.Sku).ToListAsync(ct);
        var csv = new StringBuilder("sku,nameAr,nameEn,slug,categorySlug,brandSlug,unitCode,basePrice,compareAtPrice,stockQty,status,isPrintable,isFeatured\r\n");
        foreach (var product in products)
        {
            csv.AppendJoin(',', new[]
            {
                Csv(product.Sku), Csv(product.NameAr), Csv(product.NameEn), Csv(product.Slug), Csv(product.Category.Slug),
                Csv(product.Brand?.Slug), Csv(product.Unit.Code), product.BasePrice.ToString(CultureInfo.InvariantCulture),
                product.CompareAtPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                product.StockQty.ToString(CultureInfo.InvariantCulture), product.Status.ToString(),
                product.IsPrintable ? "true" : "false", product.IsFeatured ? "true" : "false",
            }).Append("\r\n");
        }

        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(content, "text/csv; charset=utf-8", $"mohandseto-products-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    [HttpPost("products/import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ImportProducts([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0) throw ApiException.BadRequest("ملف الاستيراد فارغ");
        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
        var lines = new List<string>();
        while (await reader.ReadLineAsync(ct) is { } line) if (!string.IsNullOrWhiteSpace(line)) lines.Add(line);
        if (lines.Count < 2) throw ApiException.BadRequest("ملف الاستيراد لا يحتوي على منتجات");

        var headers = ParseCsvLine(lines[0]).Select((name, index) => (name: name.Trim().TrimStart('\uFEFF'), index))
            .ToDictionary(item => item.name, item => item.index, StringComparer.OrdinalIgnoreCase);
        var required = new[] { "sku", "nameAr", "nameEn", "slug", "categorySlug", "unitCode", "basePrice", "stockQty" };
        if (required.Any(name => !headers.ContainsKey(name)))
            throw ApiException.BadRequest("أعمدة ملف الاستيراد غير مكتملة");

        var categories = await db.Categories.ToDictionaryAsync(c => c.Slug, StringComparer.OrdinalIgnoreCase, ct);
        var brands = await db.Brands.IgnoreQueryFilters().ToDictionaryAsync(b => b.Slug, StringComparer.OrdinalIgnoreCase, ct);
        var units = await db.Units.ToDictionaryAsync(u => u.Code, StringComparer.OrdinalIgnoreCase, ct);
        var products = await db.Products.ToDictionaryAsync(p => p.Sku, StringComparer.OrdinalIgnoreCase, ct);
        var errors = new List<string>();
        var created = 0; var updated = 0;

        for (var rowIndex = 1; rowIndex < lines.Count; rowIndex++)
        {
            var values = ParseCsvLine(lines[rowIndex]);
            string Value(string name) => headers.TryGetValue(name, out var index) && index < values.Count ? values[index].Trim() : string.Empty;
            var sku = Value("sku"); var categorySlug = Value("categorySlug"); var unitCode = Value("unitCode");
            if (string.IsNullOrWhiteSpace(sku) || !categories.TryGetValue(categorySlug, out var category) || !units.TryGetValue(unitCode, out var unit)
                || !decimal.TryParse(Value("basePrice"), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0
                || !int.TryParse(Value("stockQty"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) || stock < 0)
            {
                errors.Add($"الصف {rowIndex + 1}: بيانات أساسية غير صالحة");
                continue;
            }

            var isNew = !products.TryGetValue(sku, out var product);
            product ??= new Product { Sku = sku };
            product.NameAr = Value("nameAr"); product.NameEn = Value("nameEn"); product.Slug = Value("slug");
            product.CategoryId = category.Id; product.UnitId = unit.Id; product.BasePrice = price; product.StockQty = stock;
            var brandSlug = Value("brandSlug"); product.BrandId = brands.TryGetValue(brandSlug, out var brand) ? brand.Id : null;
            product.CompareAtPrice = decimal.TryParse(Value("compareAtPrice"), NumberStyles.Number, CultureInfo.InvariantCulture, out var compareAt) ? compareAt : null;
            product.Status = Enum.TryParse<ProductStatus>(Value("status"), true, out var status) ? status : ProductStatus.Active;
            product.IsPrintable = bool.TryParse(Value("isPrintable"), out var printable) && printable;
            product.IsFeatured = bool.TryParse(Value("isFeatured"), out var featured) && featured;
            if (isNew) { db.Products.Add(product); products[sku] = product; created++; } else updated++;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new { created, updated, rejected = errors.Count, errors = errors.Take(20) });
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

    [HttpGet("products/{id:guid}/content")]
    public async Task<IActionResult> ProductContent(Guid id, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        return Ok(new
        {
            attributes = await db.ProductAttributeValues.AsNoTracking().Where(a => a.ProductId == id).OrderBy(a => a.SortOrder)
                .Select(a => new { a.NameAr, a.ValueAr, a.SortOrder }).ToListAsync(ct),
            priceTiers = await db.QuantityPriceTiers.AsNoTracking().Where(t => t.ProductId == id).OrderBy(t => t.MinQty)
                .Select(t => new { t.MinQty, t.UnitPrice }).ToListAsync(ct),
            images = await db.ProductImages.AsNoTracking().Where(i => i.ProductId == id).OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                .Select(i => new { i.Id, i.Path, i.AltAr, i.IsPrimary, i.SortOrder }).ToListAsync(ct),
            documents = await db.ProductDocuments.AsNoTracking().Where(d => d.ProductId == id).OrderBy(d => d.NameAr)
                .Select(d => new { d.Id, d.NameAr, d.Path, d.ContentType }).ToListAsync(ct),
        });
    }

    [HttpPut("products/{id:guid}/attributes")]
    public async Task<IActionResult> ReplaceAttributes(Guid id, IReadOnlyList<AttributeDto> attributes, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        if (attributes.Any(a => string.IsNullOrWhiteSpace(a.NameAr) || string.IsNullOrWhiteSpace(a.ValueAr)))
            throw ApiException.BadRequest("اسم الخاصية وقيمتها مطلوبان");
        var existing = await db.ProductAttributeValues.Where(a => a.ProductId == id).ToListAsync(ct);
        db.ProductAttributeValues.RemoveRange(existing);
        db.ProductAttributeValues.AddRange(attributes.Select((a, index) => new ProductAttributeValue
        {
            ProductId = id, NameAr = a.NameAr.Trim(), ValueAr = a.ValueAr.Trim(), SortOrder = index,
        }));
        await db.SaveChangesAsync(ct);
        return Ok(new { count = attributes.Count });
    }

    [HttpPost("products/{id:guid}/images")]
    [RequestSizeLimit(MaxImageBytes + 1024)]
    public async Task<IActionResult> UploadImage(Guid id, [FromForm] IFormFile file, [FromForm] string? altAr, [FromForm] bool isPrimary, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        ValidateFile(file, MaxImageBytes, ImageExtensions, ImageContentTypes, "الصورة");
        var storedPath = await StoreFileAsync(id, "images", file, ct);
        if (isPrimary)
        {
            var currentPrimary = await db.ProductImages.Where(i => i.ProductId == id && i.IsPrimary).ToListAsync(ct);
            currentPrimary.ForEach(i => i.IsPrimary = false);
        }
        var image = new ProductImage
        {
            ProductId = id, Path = storedPath, AltAr = altAr?.Trim(), IsPrimary = isPrimary,
            SortOrder = await db.ProductImages.CountAsync(i => i.ProductId == id, ct),
        };
        db.ProductImages.Add(image); await db.SaveChangesAsync(ct);
        return Ok(new { image.Id, image.Path, image.AltAr, image.IsPrimary, image.SortOrder });
    }

    [HttpDelete("products/{productId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid productId, Guid imageId, CancellationToken ct)
    {
        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, ct)
            ?? throw ApiException.NotFound("الصورة غير موجودة");
        DeleteStoredFile(image.Path); db.ProductImages.Remove(image); await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("products/{id:guid}/documents")]
    [RequestSizeLimit(MaxDocumentBytes + 1024)]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] IFormFile file, [FromForm] string nameAr, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        if (string.IsNullOrWhiteSpace(nameAr)) throw ApiException.BadRequest("اسم المستند مطلوب");
        ValidateFile(file, MaxDocumentBytes, [".pdf"], ["application/pdf"], "المستند");
        var document = new ProductDocument
        {
            ProductId = id, NameAr = nameAr.Trim(), Path = await StoreFileAsync(id, "documents", file, ct), ContentType = file.ContentType,
        };
        db.ProductDocuments.Add(document); await db.SaveChangesAsync(ct);
        return Ok(new { document.Id, document.NameAr, document.Path, document.ContentType });
    }

    [HttpDelete("products/{productId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid productId, Guid documentId, CancellationToken ct)
    {
        var document = await db.ProductDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.ProductId == productId, ct)
            ?? throw ApiException.NotFound("المستند غير موجود");
        DeleteStoredFile(document.Path); db.ProductDocuments.Remove(document); await db.SaveChangesAsync(ct);
        return NoContent();
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

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> ArchiveCategory(Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw ApiException.NotFound("القسم غير موجود");
        if (await db.Products.AnyAsync(p => p.CategoryId == id && p.Status == ProductStatus.Active, ct))
            throw ApiException.Conflict("لا يمكن تعطيل قسم يحتوي على منتجات نشطة");
        if (await db.Categories.AnyAsync(c => c.ParentId == id && c.IsActive, ct))
            throw ApiException.Conflict("عطّل الأقسام الفرعية أولًا");
        category.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("brands")]
    public async Task<IActionResult> Brands(CancellationToken ct) => Ok(await db.Brands.IgnoreQueryFilters()
        .AsNoTracking().OrderBy(b => b.NameAr).Select(b => new { b.Id, b.NameAr, b.NameEn, b.Slug, b.LogoPath, b.IsActive })
        .ToListAsync(ct));

    [HttpPost("brands")]
    public async Task<IActionResult> CreateBrand(UpsertBrandDto dto, CancellationToken ct)
    {
        await ValidateBrandAsync(dto, null, ct);
        var brand = Map(new Brand(), dto);
        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);
        return Created($"/api/admin/catalog/brands/{brand.Id}", new { brand.Id });
    }

    [HttpPut("brands/{id:guid}")]
    public async Task<IActionResult> UpdateBrand(Guid id, UpsertBrandDto dto, CancellationToken ct)
    {
        var brand = await db.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw ApiException.NotFound("العلامة التجارية غير موجودة");
        await ValidateBrandAsync(dto, id, ct);
        Map(brand, dto);
        await db.SaveChangesAsync(ct);
        return Ok(new { brand.Id });
    }

    [HttpDelete("brands/{id:guid}")]
    public async Task<IActionResult> ArchiveBrand(Guid id, CancellationToken ct)
    {
        var brand = await db.Brands.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw ApiException.NotFound("العلامة التجارية غير موجودة");
        brand.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("products/{id:guid}/variants")]
    public async Task<IActionResult> Variants(Guid id, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        return Ok(await db.ProductVariants.AsNoTracking().Where(v => v.ProductId == id).OrderBy(v => v.NameAr)
            .Select(v => new { v.Id, v.Sku, v.NameAr, v.NameEn, v.OptionsJson, v.PriceAdjustment, v.StockQty, v.IsActive })
            .ToListAsync(ct));
    }

    [HttpPut("products/{id:guid}/variants")]
    public async Task<IActionResult> ReplaceVariants(Guid id, IReadOnlyList<UpsertVariantDto> variants, CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Id == id, ct)) throw ApiException.NotFound("المنتج غير موجود");
        if (variants.Any(v => string.IsNullOrWhiteSpace(v.Sku) || string.IsNullOrWhiteSpace(v.NameAr) || v.StockQty < 0))
            throw ApiException.BadRequest("بيانات أحد المتغيرات غير صالحة");
        if (variants.Select(v => v.Sku.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != variants.Count)
            throw ApiException.BadRequest("أكواد المتغيرات يجب أن تكون فريدة");
        var requestedSkus = variants.Select(v => v.Sku.Trim()).ToList();
        if (await db.ProductVariants.IgnoreQueryFilters().AnyAsync(v => v.ProductId != id && requestedSkus.Contains(v.Sku), ct))
            throw ApiException.Conflict("أحد أكواد المتغيرات مستخدم في منتج آخر");
        var existing = await db.ProductVariants.Where(v => v.ProductId == id).ToListAsync(ct);
        db.ProductVariants.RemoveRange(existing);
        db.ProductVariants.AddRange(variants.Select(v => new ProductVariant
        {
            ProductId = id, Sku = v.Sku.Trim(), NameAr = v.NameAr.Trim(), NameEn = v.NameEn.Trim(),
            OptionsJson = v.OptionsJson, PriceAdjustment = v.PriceAdjustment, StockQty = v.StockQty, IsActive = v.IsActive,
        }));
        await db.SaveChangesAsync(ct);
        return Ok(new { count = variants.Count });
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

    private async Task ValidateBrandAsync(UpsertBrandDto dto, Guid? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NameAr) || string.IsNullOrWhiteSpace(dto.Slug))
            throw ApiException.BadRequest("اسم العلامة والرابط المختصر مطلوبان");
        if (await db.Brands.IgnoreQueryFilters().AnyAsync(b => b.Id != id && b.Slug == dto.Slug.Trim(), ct))
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

    private static Brand Map(Brand brand, UpsertBrandDto dto)
    {
        brand.NameAr = dto.NameAr.Trim(); brand.NameEn = dto.NameEn.Trim(); brand.Slug = dto.Slug.Trim();
        brand.LogoPath = dto.LogoPath?.Trim(); brand.IsActive = dto.IsActive;
        return brand;
    }

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private static IReadOnlyList<string> ParseCsvLine(string line)
    {
        var values = new List<string>(); var value = new StringBuilder(); var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"') { value.Append('"'); index++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
            else value.Append(character);
        }
        values.Add(value.ToString());
        return values;
    }

    private static void ValidateFile(IFormFile file, long maxBytes, IReadOnlyCollection<string> extensions, IReadOnlyCollection<string> contentTypes, string label)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length is 0 || file.Length > maxBytes) throw ApiException.BadRequest($"حجم {label} غير صالح");
        if (!extensions.Contains(extension) || !contentTypes.Contains(file.ContentType)) throw ApiException.BadRequest($"صيغة {label} غير مدعومة");
    }

    private async Task<string> StoreFileAsync(Guid productId, string folder, IFormFile file, CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var relative = Path.Combine("storage", "catalog", productId.ToString(), folder, $"{Guid.NewGuid():N}{extension}");
        var fullPath = SafeStoragePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream, ct);
        return relative.Replace('\\', '/');
    }

    private void DeleteStoredFile(string path)
    {
        if (!path.StartsWith("storage/catalog/", StringComparison.OrdinalIgnoreCase)) return;
        var fullPath = SafeStoragePath(path);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    private string SafeStoragePath(string relative)
    {
        var storageRoot = Path.GetFullPath(Path.Combine(ContentRoot, "storage", "catalog"));
        var fullPath = Path.GetFullPath(Path.Combine(ContentRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(storageRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw ApiException.BadRequest("مسار تخزين غير صالح");
        return fullPath;
    }
}
