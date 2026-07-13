using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Shopping;

public sealed class CartService(AppDbContext db, ITenantProvider tenantProvider)
{
    public async Task<CartDto> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await LoadCartAsync(userId, ct);
        return cart is null ? Empty() : await MapAsync(cart, ct);
    }

    public async Task<CartDto> AddAsync(Guid userId, AddCartItemDto dto, CancellationToken ct = default)
    {
        var tenantId = TenantId();
        var product = await db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.Status == ProductStatus.Active, ct)
            ?? throw ApiException.NotFound("المنتج غير موجود");
        ProductVariant? variant = null;
        if (dto.VariantId is { } variantId)
            variant = product.Variants.FirstOrDefault(v => v.Id == variantId && v.IsActive)
                ?? throw ApiException.BadRequest("اختيار المنتج غير صالح");
        var minimum = product.MinOrderQty;
        var quantity = Math.Max(dto.Quantity, minimum);
        var available = variant?.StockQty ?? product.StockQty;
        if (available <= 0 || quantity > available) throw ApiException.Conflict("الكمية المطلوبة غير متاحة حاليًا");
        if (product.MaxOrderQty is { } max && quantity > max) throw ApiException.BadRequest($"أقصى كمية للطلب هي {max}");

        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);
        if (cart is null)
        {
            cart = new Cart { TenantId = tenantId, UserId = userId };
            db.Carts.Add(cart);
        }
        var item = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId && i.VariantId == dto.VariantId && !i.IsSavedForLater && i.CustomizationJson == dto.CustomizationJson);
        if (item is null)
            cart.Items.Add(new CartItem { TenantId = tenantId, ProductId = dto.ProductId, VariantId = dto.VariantId, Quantity = quantity, CustomizationJson = dto.CustomizationJson });
        else
        {
            var combined = item.Quantity + quantity;
            if (combined > available) throw ApiException.Conflict("إجمالي الكمية يتجاوز المخزون المتاح");
            item.Quantity = combined;
        }
        await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> AddCustomRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default)
    {
        var tenantId = TenantId();
        var request = await db.CustomProductRequests.Include(r => r.Template).ThenInclude(t => t.Product).Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status != CustomRequestStatus.DesignApproved || request.QuotedTotal is null)
            throw ApiException.Conflict("يجب اعتماد التصميم وعرض السعر قبل الإضافة للسلة");
        if (request.QuoteExpiresAt <= DateTime.UtcNow) throw ApiException.Conflict("انتهت صلاحية عرض السعر");
        var source = request.Items.Single();
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);
        if (cart is null) { cart = new Cart { TenantId = tenantId, UserId = userId }; db.Carts.Add(cart); }
        if (cart.Items.Any(i => i.CustomProductRequestId == request.Id)) throw ApiException.Conflict("المنتج المخصص موجود في السلة بالفعل");
        db.CartItems.Add(new CartItem { TenantId = tenantId, CartId = cart.Id, ProductId = request.Template.ProductId, Quantity = source.Quantity,
            CustomProductRequestId = request.Id, CustomUnitPrice = request.QuotedTotal.Value / source.Quantity,
            CustomLineTotal = request.QuotedTotal.Value,
            CustomizationJson = JsonSerializer.Serialize(new { customProductRequestId = request.Id, request.Number,
                source.OptionId, source.PrintMethodId, source.MaterialId, source.ColorId, source.SizeId,
                source.PrintWidthCm, source.PrintHeightCm, source.PrintColorCount, source.CustomText }) });
        request.Status = CustomRequestStatus.AwaitingCheckout;
        await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> UpdateAsync(Guid userId, Guid itemId, int quantity, CancellationToken ct = default)
    {
        var item = await db.CartItems.Include(i => i.Cart).Include(i => i.Product).Include(i => i.Variant)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId && i.Cart.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("عنصر السلة غير موجود");
        if (item.CustomProductRequestId is not null) throw ApiException.Conflict("كمية المنتج المخصص ثابتة حسب عرض السعر");
        if (quantity < item.Product.MinOrderQty) throw ApiException.BadRequest($"أقل كمية للطلب هي {item.Product.MinOrderQty}");
        var available = item.Variant?.StockQty ?? item.Product.StockQty;
        if (quantity > available) throw ApiException.Conflict("الكمية المطلوبة غير متاحة");
        if (item.Product.MaxOrderQty is { } max && quantity > max) throw ApiException.BadRequest($"أقصى كمية للطلب هي {max}");
        item.Quantity = quantity; await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> RemoveAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await OwnedItemAsync(userId, itemId, ct);
        if (item.CustomProductRequestId is { } requestId)
        {
            var request = await db.CustomProductRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId, ct);
            if (request?.Status == CustomRequestStatus.AwaitingCheckout) request.Status = CustomRequestStatus.DesignApproved;
        }
        db.CartItems.Remove(item); await db.SaveChangesAsync(ct);
        return await GetAsync(userId, ct);
    }

    public async Task<CartDto> SetSavedAsync(Guid userId, Guid itemId, bool saved, CancellationToken ct = default)
    {
        var item = await OwnedItemAsync(userId, itemId, ct);
        item.IsSavedForLater = saved; await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task ClearAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);
        if (cart is null) return;
        db.CartItems.RemoveRange(cart.Items.Where(i => !i.IsSavedForLater)); await db.SaveChangesAsync(ct);
    }

    private async Task<CartItem> OwnedItemAsync(Guid userId, Guid itemId, CancellationToken ct) =>
        await db.CartItems.Include(i => i.Cart).FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId && i.Cart.Status == CartStatus.Active, ct)
        ?? throw ApiException.NotFound("عنصر السلة غير موجود");

    private Task<Cart?> LoadCartAsync(Guid userId, CancellationToken ct) => db.Carts.AsNoTracking()
        .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
        .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
        .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
        .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.PriceTiers)
        .Include(c => c.Items).ThenInclude(i => i.Variant)
        .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);

    private async Task<CartDto> MapAsync(Cart cart, CancellationToken ct)
    {
        var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();
        var now = DateTime.UtcNow;
        var contractPrices = await db.CompanyProductPrices.AsNoTracking()
            .Where(p => productIds.Contains(p.ProductId) && (p.ValidFrom == null || p.ValidFrom <= now) && (p.ValidTo == null || p.ValidTo >= now))
            .GroupBy(p => p.ProductId).Select(g => g.OrderByDescending(p => p.ValidFrom).First())
            .ToDictionaryAsync(p => p.ProductId, p => p.ContractPrice, ct);
        CartItemDto Map(CartItem item)
        {
            var product = item.Product;
            var basePrice = contractPrices.TryGetValue(product.Id, out var contract) ? contract : product.BasePrice;
            var tier = product.PriceTiers.Where(t => t.MinQty <= item.Quantity).OrderByDescending(t => t.MinQty).FirstOrDefault();
            var unitPrice = item.CustomLineTotal is { } customTotal ? customTotal / item.Quantity
                : item.CustomUnitPrice ?? (contractPrices.ContainsKey(product.Id) ? basePrice : tier?.UnitPrice ?? basePrice);
            if (item.CustomUnitPrice is null) unitPrice += item.Variant?.PriceAdjustment ?? 0;
            var line = item.CustomLineTotal ?? unitPrice * item.Quantity;
            var before = item.CustomUnitPrice is not null ? line : (product.BasePrice + (item.Variant?.PriceAdjustment ?? 0)) * item.Quantity;
            var primary = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).FirstOrDefault();
            var image = primary is { Path: var path } && path.StartsWith("storage/catalog/", StringComparison.OrdinalIgnoreCase)
                ? $"/api/catalog/media/images/{primary.Id}" : primary?.Path;
            var available = item.Variant?.StockQty ?? product.StockQty;
            return new(item.Id, product.Id, product.Slug, item.Variant?.Sku ?? product.Sku, product.NameAr, item.Variant?.NameAr,
                item.Quantity, product.MinOrderQty, available, unitPrice, line, Math.Max(0, before - line), product.Unit.NameAr,
                available <= 0 ? StockStatus.OutOfStock.ToString() : available <= product.LowStockThreshold ? StockStatus.LowStock.ToString() : StockStatus.InStock.ToString(),
                image, item.IsSavedForLater, item.CustomProductRequestId);
        }
        var mapped = cart.Items.Select(Map).ToList();
        var active = mapped.Where(i => !i.IsSavedForLater).ToList();
        var beforeSavings = active.Sum(i => i.LineTotal + i.Savings);
        var savings = active.Sum(i => i.Savings); var subtotal = active.Sum(i => i.LineTotal);
        var shipping = subtotal == 0 || subtotal >= 2000 ? 0 : 150;
        var total = subtotal + shipping;
        var tax = active.Sum(i => i.LineTotal * cart.Items.First(x => x.Id == i.Id).Product.TaxRatePercent / (100 + cart.Items.First(x => x.Id == i.Id).Product.TaxRatePercent));
        return new(cart.Id, active, mapped.Where(i => i.IsSavedForLater).ToList(), active.Count, active.Sum(i => i.Quantity),
            beforeSavings, savings, subtotal, decimal.Round(tax, 2), shipping, total, subtotal >= 2000);
    }

    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static CartDto Empty() => new(null, [], [], 0, 0, 0, 0, 0, 0, 0, 0, false);
}
