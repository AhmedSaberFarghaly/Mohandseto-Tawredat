using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class Category : BaseEntity
{
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Category> Children { get; set; } = [];
}

public class Brand : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UnitOfMeasure : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
}

public enum ProductStatus { Draft, Active, Archived }
public enum StockStatus { InStock, LowStock, OutOfStock, Backorder }

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid? BrandId { get; set; }
    public Brand? Brand { get; set; }
    public Guid UnitId { get; set; }
    public UnitOfMeasure Unit { get; set; } = null!;
    public decimal BasePrice { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal TaxRatePercent { get; set; } = 14m;
    public int MinOrderQty { get; set; } = 1;
    public int? MaxOrderQty { get; set; }
    public int StockQty { get; set; }
    public int LowStockThreshold { get; set; } = 10;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public bool IsPrintable { get; set; }
    public bool IsFeatured { get; set; }
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public string? WarrantyAr { get; set; }
    public int DeliveryEstimateDays { get; set; } = 2;
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<QuantityPriceTier> PriceTiers { get; set; } = [];
    public ICollection<ProductAttributeValue> Attributes { get; set; } = [];
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductDocument> Documents { get; set; } = [];

    public StockStatus GetStockStatus() =>
        StockQty <= 0 ? StockStatus.OutOfStock
        : StockQty <= LowStockThreshold ? StockStatus.LowStock
        : StockStatus.InStock;
}

public class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Path { get; set; } = string.Empty;
    public string? AltAr { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}

public class QuantityPriceTier : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int MinQty { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ProductAttributeValue : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string NameAr { get; set; } = string.Empty;
    public string ValueAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? OptionsJson { get; set; }
    public decimal PriceAdjustment { get; set; }
    public int StockQty { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductDocument : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string NameAr { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}

/// <summary>Company-specific contract price for a product (overrides base/tier prices).</summary>
public class CompanyProductPrice : TenantEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal ContractPrice { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class Favorite : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}

public class RecentlyViewed : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
}

public class CompareItem : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
