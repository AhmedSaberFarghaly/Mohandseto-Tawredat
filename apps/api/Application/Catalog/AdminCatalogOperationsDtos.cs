namespace Mohandseto.Api.Application.Catalog;

public record ProductCommercialDto(Guid Id, string Sku, string Name, decimal BasePrice, decimal CostPrice, decimal ProfitMargin,
    string? PackageType, int UnitsPerPackage, int PackagesPerCarton, string? CartonBarcode, string? Warranty,
    string? SeoTitle, string? SeoDescription, string? SeoKeywords, string Status,
    IReadOnlyList<ProductLinkDto> Links, IReadOnlyList<CompanyProductPriceDto> CompanyPrices,
    IReadOnlyList<CatalogProductOptionDto> Products, IReadOnlyList<CompanyOptionDto> Companies);
public record ProductLinkDto(Guid ProductId, string Sku, string Name, string Type, int SortOrder);
public record CompanyProductPriceDto(Guid TenantId, string Company, decimal Price, DateTime? ValidFrom, DateTime? ValidTo);
public record CatalogProductOptionDto(Guid Id, string Sku, string Name, decimal Price);
public record CompanyOptionDto(Guid TenantId, string Name);
public record SaveProductCommercialDto(decimal CostPrice, string? PackageType, int UnitsPerPackage, int PackagesPerCarton,
    string? CartonBarcode, string? Warranty, string? SeoTitle, string? SeoDescription, string? SeoKeywords);
public record ReplaceProductLinksDto(IReadOnlyList<SaveProductLinkDto> Links);
public record SaveProductLinkDto(Guid ProductId, string Type, int SortOrder);
public record ReplaceCompanyPricesDto(IReadOnlyList<SaveCompanyPriceDto> Prices);
public record SaveCompanyPriceDto(Guid TenantId, decimal Price, DateTime? ValidFrom, DateTime? ValidTo);
public record BulkPriceUpdateDto(IReadOnlyList<BulkPriceLineDto> Items, string Reason);
public record BulkPriceLineDto(Guid ProductId, decimal NewPrice);
public record ProductPriceChangeDto(Guid Id, Guid ProductId, string Sku, string Product, string Staff, decimal OldPrice,
    decimal NewPrice, decimal ChangePercent, string Reason, string Source, DateTime At);
public record SetProductStatusDto(string Status);
