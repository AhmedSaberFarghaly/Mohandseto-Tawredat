using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Catalog;

public sealed class AdminCatalogOperationsService(AppDbContext db)
{
    public async Task<ProductCommercialDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("المنتج غير موجود");
        var links = await db.ProductLinks.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.Type).ThenBy(x => x.SortOrder).ToListAsync(ct);
        var linkedIds = links.Select(x => x.LinkedProductId).ToList(); var linked = await db.Products.AsNoTracking().Where(x => linkedIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var prices = await db.CompanyProductPrices.AsNoTracking().Where(x => x.ProductId == id).OrderBy(x => x.ValidFrom).ToListAsync(ct);
        var tenantIds = prices.Select(x => x.TenantId).ToList(); var companiesByTenant = await db.Companies.AsNoTracking().Where(x => tenantIds.Contains(x.TenantId)).ToDictionaryAsync(x => x.TenantId, x => x.LegalName, ct);
        var options = await db.Products.AsNoTracking().Where(x => x.Id != id && x.Status == ProductStatus.Active).OrderBy(x => x.NameAr).Take(250)
            .Select(x => new CatalogProductOptionDto(x.Id, x.Sku, x.NameAr, x.BasePrice)).ToListAsync(ct);
        var companies = await db.Companies.AsNoTracking().OrderBy(x => x.LegalName).Select(x => new CompanyOptionDto(x.TenantId, x.LegalName)).ToListAsync(ct);
        var margin = product.BasePrice == 0 ? 0 : decimal.Round((product.BasePrice - product.CostPrice) / product.BasePrice * 100, 2);
        return new(product.Id, product.Sku, product.NameAr, product.BasePrice, product.CostPrice, margin, product.PackageType,
            product.UnitsPerPackage, product.PackagesPerCarton, product.CartonBarcode, product.WarrantyAr, product.SeoTitle,
            product.SeoDescription, product.SeoKeywords, product.Status.ToString(), links.Where(x => linked.ContainsKey(x.LinkedProductId))
                .Select(x => new ProductLinkDto(x.LinkedProductId, linked[x.LinkedProductId].Sku, linked[x.LinkedProductId].NameAr, x.Type.ToString(), x.SortOrder)).ToList(),
            prices.Select(x => new CompanyProductPriceDto(x.TenantId, companiesByTenant.GetValueOrDefault(x.TenantId, "شركة"), x.ContractPrice, x.ValidFrom, x.ValidTo)).ToList(), options, companies);
    }

    public async Task<ProductCommercialDto> SaveAsync(Guid id, SaveProductCommercialDto dto, CancellationToken ct = default)
    {
        var product = await Product(id, ct); if (dto.CostPrice < 0 || dto.UnitsPerPackage < 1 || dto.PackagesPerCarton < 1) throw ApiException.BadRequest("بيانات التكلفة أو العبوات غير صالحة");
        product.CostPrice = dto.CostPrice; product.PackageType = Clean(dto.PackageType, 100); product.UnitsPerPackage = dto.UnitsPerPackage;
        product.PackagesPerCarton = dto.PackagesPerCarton; product.CartonBarcode = Clean(dto.CartonBarcode, 100);
        product.WarrantyAr = Clean(dto.Warranty, 1000); product.SeoTitle = Clean(dto.SeoTitle, 180);
        product.SeoDescription = Clean(dto.SeoDescription, 500); product.SeoKeywords = Clean(dto.SeoKeywords, 500);
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<ProductCommercialDto> ReplaceLinksAsync(Guid id, ReplaceProductLinksDto dto, CancellationToken ct = default)
    {
        var product = await Product(id, ct); var parsed = dto.Links.Select(x => (Input: x, Type: Enum.TryParse<ProductLinkType>(x.Type, true, out var type) ? type : (ProductLinkType?)null)).ToList();
        if (parsed.Any(x => x.Type is null || x.Input.ProductId == id) || parsed.Select(x => new { x.Input.ProductId, x.Type }).Distinct().Count() != parsed.Count)
            throw ApiException.BadRequest("روابط المنتجات غير صالحة");
        var ids = parsed.Select(x => x.Input.ProductId).Distinct().ToList(); if (await db.Products.CountAsync(x => ids.Contains(x.Id) && x.Status == ProductStatus.Active, ct) != ids.Count) throw ApiException.BadRequest("أحد المنتجات المرتبطة غير متاح");
        db.ProductLinks.RemoveRange(await db.ProductLinks.Where(x => x.ProductId == id).ToListAsync(ct));
        db.ProductLinks.AddRange(parsed.Select(x => new ProductLink { ProductId = product.Id, LinkedProductId = x.Input.ProductId, Type = x.Type!.Value, SortOrder = x.Input.SortOrder }));
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<ProductCommercialDto> ReplaceCompanyPricesAsync(Guid id, ReplaceCompanyPricesDto dto, CancellationToken ct = default)
    {
        var product = await Product(id, ct); if (dto.Prices.Any(x => x.Price <= 0 || x.ValidTo is not null && x.ValidFrom is not null && x.ValidTo <= x.ValidFrom) || dto.Prices.Select(x => new { x.TenantId, x.ValidFrom }).Distinct().Count() != dto.Prices.Count)
            throw ApiException.BadRequest("أسعار الشركات غير صالحة");
        var tenants = dto.Prices.Select(x => x.TenantId).Distinct().ToList(); if (await db.Companies.CountAsync(x => tenants.Contains(x.TenantId), ct) != tenants.Count) throw ApiException.BadRequest("إحدى الشركات غير موجودة");
        db.CompanyProductPrices.RemoveRange(await db.CompanyProductPrices.Where(x => x.ProductId == id).ToListAsync(ct));
        db.CompanyProductPrices.AddRange(dto.Prices.Select(x => new CompanyProductPrice { TenantId = x.TenantId, ProductId = product.Id, ContractPrice = x.Price, ValidFrom = x.ValidFrom, ValidTo = x.ValidTo }));
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<List<ProductPriceChangeDto>> BulkPricesAsync(Guid staffId, BulkPriceUpdateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Items.Count == 0 || dto.Items.Any(x => x.NewPrice <= 0) || dto.Items.Select(x => x.ProductId).Distinct().Count() != dto.Items.Count)
            throw ApiException.BadRequest("بيانات تعديل الأسعار غير صالحة");
        var ids = dto.Items.Select(x => x.ProductId).ToList(); var products = await db.Products.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        if (products.Count != ids.Count) throw ApiException.NotFound("أحد المنتجات غير موجود");
        foreach (var input in dto.Items)
        {
            var product = products[input.ProductId]; if (product.BasePrice == input.NewPrice) continue;
            db.ProductPriceChanges.Add(new ProductPriceChange { ProductId = product.Id, StaffUserId = staffId, OldPrice = product.BasePrice,
                NewPrice = input.NewPrice, Reason = Required(dto.Reason, 500), Source = "Bulk" }); product.BasePrice = input.NewPrice;
        }
        await db.SaveChangesAsync(ct); return await HistoryAsync(ids, 100, ct);
    }

    public async Task RecordPriceChangeAsync(Guid staffId, Product product, decimal oldPrice, string source, string reason, CancellationToken ct = default)
    {
        if (oldPrice == product.BasePrice) return; db.ProductPriceChanges.Add(new ProductPriceChange { ProductId = product.Id, StaffUserId = staffId,
            OldPrice = oldPrice, NewPrice = product.BasePrice, Reason = reason, Source = source }); await db.SaveChangesAsync(ct);
    }

    public async Task<List<ProductPriceChangeDto>> HistoryAsync(IReadOnlyList<Guid>? productIds, int limit = 200, CancellationToken ct = default)
    {
        var query = db.ProductPriceChanges.AsNoTracking(); if (productIds is { Count: > 0 }) query = query.Where(x => productIds.Contains(x.ProductId));
        var rows = await query.OrderByDescending(x => x.CreatedAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct);
        var productIdsFound = rows.Select(x => x.ProductId).Distinct().ToList(); var staffIds = rows.Select(x => x.StaffUserId).Distinct().ToList();
        var products = await db.Products.IgnoreQueryFilters().AsNoTracking().Where(x => productIdsFound.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var staff = await db.Users.AsNoTracking().Where(x => staffIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        return rows.Select(x => new ProductPriceChangeDto(x.Id, x.ProductId, products.GetValueOrDefault(x.ProductId)?.Sku ?? "—",
            products.GetValueOrDefault(x.ProductId)?.NameAr ?? "منتج", staff.GetValueOrDefault(x.StaffUserId, "موظف"), x.OldPrice, x.NewPrice,
            x.OldPrice == 0 ? 0 : decimal.Round((x.NewPrice - x.OldPrice) / x.OldPrice * 100, 2), x.Reason, x.Source, x.CreatedAt)).ToList();
    }

    public async Task SetStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var product = await Product(id, ct); if (!Enum.TryParse<ProductStatus>(status, true, out var parsed)) throw ApiException.BadRequest("حالة المنتج غير صالحة");
        product.Status = parsed; await db.SaveChangesAsync(ct);
    }

    private async Task<Product> Product(Guid id, CancellationToken ct) => await db.Products.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("المنتج غير موجود");
    private static string Required(string? value, int max) => string.IsNullOrWhiteSpace(value) ? throw ApiException.BadRequest("البيان المطلوب غير مكتمل") : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
