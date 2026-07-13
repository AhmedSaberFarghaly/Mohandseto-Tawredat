namespace Mohandseto.Api.Application.Catalog;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string NameAr,
    string NameEn,
    string Slug,
    string? IconName,
    string? ImageUrl,
    int ProductCount,
    IReadOnlyList<CategoryDto> Children);

public sealed record BrandDto(Guid Id, string NameAr, string NameEn, string Slug, string? LogoUrl);

public sealed record ProductCardDto(
    Guid Id,
    string Sku,
    string NameAr,
    string NameEn,
    string Slug,
    string CategoryName,
    string? BrandName,
    decimal Price,
    decimal? CompareAtPrice,
    bool HasContractPrice,
    string StockStatus,
    int StockQty,
    string? ImageUrl,
    double Rating,
    int RatingCount,
    int MinOrderQty,
    string UnitName,
    bool IsPrintable,
    bool IsFeatured,
    bool IsFavorite);

public sealed record PriceTierDto(int MinQty, decimal UnitPrice);
public sealed record AttributeDto(string NameAr, string ValueAr, int SortOrder);
public sealed record VariantDto(Guid Id, string Sku, string NameAr, string NameEn, string? OptionsJson, decimal Price, int StockQty, bool IsActive);
public sealed record ProductImageDto(Guid Id, string Url, string? AltAr, bool IsPrimary, int SortOrder);
public sealed record ProductDocumentDto(Guid Id, string NameAr, string Url, string ContentType);
public sealed record CompareProductDto(ProductCardDto Summary, IReadOnlyDictionary<string, string> Attributes);

public sealed record ProductDetailDto(
    ProductCardDto Summary,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal TaxRatePercent,
    int? MaxOrderQty,
    string? WarrantyAr,
    int DeliveryEstimateDays,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<PriceTierDto> PriceTiers,
    IReadOnlyList<AttributeDto> Attributes,
    IReadOnlyList<VariantDto> Variants,
    IReadOnlyList<ProductDocumentDto> Documents,
    IReadOnlyList<ProductCardDto> RelatedProducts);

public sealed class ProductQuery
{
    public string? Q { get; init; }
    public Guid? CategoryId { get; init; }
    public Guid? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Stock { get; init; }
    public bool? Featured { get; init; }
    public bool? Printable { get; init; }
    public string Sort { get; init; } = "featured";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record UpsertCategoryDto(Guid? ParentId, string NameAr, string NameEn, string Slug, string? IconName, string? ImagePath, int SortOrder, bool IsActive);
public sealed record CategoryOrderItemDto(Guid Id, int SortOrder);
public sealed record ReorderCategoriesDto(IReadOnlyList<CategoryOrderItemDto> Items);
public sealed record UpsertBrandDto(string NameAr, string NameEn, string Slug, string? LogoPath, bool IsActive);
public sealed record UpsertVariantDto(Guid? Id, string Sku, string NameAr, string NameEn, string? OptionsJson, decimal PriceAdjustment, int StockQty, bool IsActive);

public sealed record UpsertProductDto(
    string Sku,
    string NameAr,
    string NameEn,
    string Slug,
    string? DescriptionAr,
    string? DescriptionEn,
    Guid CategoryId,
    Guid? BrandId,
    Guid UnitId,
    decimal BasePrice,
    decimal? CompareAtPrice,
    decimal TaxRatePercent,
    int MinOrderQty,
    int? MaxOrderQty,
    int StockQty,
    int LowStockThreshold,
    string Status,
    bool IsPrintable,
    bool IsFeatured,
    string? WarrantyAr,
    int DeliveryEstimateDays);
