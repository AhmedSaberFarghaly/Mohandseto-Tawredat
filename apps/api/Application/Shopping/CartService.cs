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
        var product = await db.Products.Include(p => p.Variants).Include(p => p.PriceTiers).FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.Status == ProductStatus.Active, ct)
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
        {
            item = new CartItem { TenantId = tenantId, ProductId = dto.ProductId, VariantId = dto.VariantId, Quantity = quantity, CustomizationJson = dto.CustomizationJson };
            cart.Items.Add(item);
        }
        else
        {
            var combined = item.Quantity + quantity;
            if (combined > available) throw ApiException.Conflict("إجمالي الكمية يتجاوز المخزون المتاح");
            item.Quantity = combined;
        }
        item.PriceAtAdded = await CurrentUnitPriceAsync(product, variant, item.Quantity, ct);
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
            CustomLineTotal = request.QuotedTotal.Value, PriceAtAdded = request.QuotedTotal.Value / source.Quantity,
            CustomizationJson = JsonSerializer.Serialize(new { customProductRequestId = request.Id, request.Number,
                source.OptionId, source.PrintMethodId, source.MaterialId, source.ColorId, source.SizeId,
                source.PrintWidthCm, source.PrintHeightCm, source.PrintColorCount, source.CustomText }) });
        request.Status = CustomRequestStatus.AwaitingCheckout;
        await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> UpdateAsync(Guid userId, Guid itemId, int quantity, CancellationToken ct = default)
    {
        var item = await db.CartItems.Include(i => i.Cart).Include(i => i.Product).ThenInclude(p => p.PriceTiers).Include(i => i.Variant)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId && i.Cart.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("عنصر السلة غير موجود");
        if (item.CustomProductRequestId is not null) throw ApiException.Conflict("كمية المنتج المخصص ثابتة حسب عرض السعر");
        if (quantity < item.Product.MinOrderQty) throw ApiException.BadRequest($"أقل كمية للطلب هي {item.Product.MinOrderQty}");
        var available = item.Variant?.StockQty ?? item.Product.StockQty;
        if (quantity > available) throw ApiException.Conflict("الكمية المطلوبة غير متاحة");
        if (item.Product.MaxOrderQty is { } max && quantity > max) throw ApiException.BadRequest($"أقصى كمية للطلب هي {max}");
        item.Quantity = quantity;
        item.PriceAtAdded = await CurrentUnitPriceAsync(item.Product, item.Variant, quantity, ct);
        await db.SaveChangesAsync(ct);
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

    public async Task<CartDto> SetItemNoteAsync(Guid userId, Guid itemId, string? note, CancellationToken ct = default)
    {
        var item = await OwnedItemAsync(userId, itemId, ct);
        item.CustomerNote = Clean(note, 500); await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> SetOrderNoteAsync(Guid userId, string? note, CancellationToken ct = default)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("السلة غير موجودة");
        cart.OrderNote = Clean(note, 1500); await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> ApplyCouponAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.PriceTiers)
            .Include(c => c.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("السلة غير موجودة");
        var normalized = code.Trim().ToUpperInvariant(); var now = DateTime.UtcNow;
        var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == normalized && c.IsActive, ct)
            ?? throw ApiException.BadRequest("كود الخصم غير صالح");
        if (coupon.StartsAt > now || coupon.ExpiresAt < now || coupon.UsageLimit is { } limit && coupon.UsedCount >= limit)
            throw ApiException.Conflict("كود الخصم غير متاح حاليًا");
        if (coupon.OncePerCompany && await db.Orders.AnyAsync(x => x.CouponCode == normalized && x.Status != OrderStatus.Cancelled, ct))
            throw ApiException.Conflict("تم استخدام هذا الكوبون للشركة من قبل");
        if (coupon.NewCustomersOnly && await db.Orders.AnyAsync(x => x.Status != OrderStatus.Cancelled, ct))
            throw ApiException.Conflict("هذا الكوبون متاح للعملاء الجدد فقط");
        var categoryIds = ParseCategoryIds(coupon.ApplicableCategoryIds);
        if (categoryIds.Count > 0 && !cart.Items.Any(x => !x.IsSavedForLater && categoryIds.Contains(x.Product.CategoryId)))
            throw ApiException.Conflict("الكوبون لا ينطبق على أصناف السلة الحالية");
        decimal subtotal = 0;
        foreach (var item in cart.Items.Where(i => !i.IsSavedForLater))
            subtotal += item.CustomLineTotal ?? await CurrentUnitPriceAsync(item.Product, item.Variant, item.Quantity, ct) * item.Quantity;
        if (subtotal < coupon.MinimumSubtotal)
            throw ApiException.Conflict($"الحد الأدنى لاستخدام الكود هو {coupon.MinimumSubtotal:0.##} ج.م");
        cart.CouponCode = normalized; await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> RemoveCouponAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("السلة غير موجودة");
        cart.CouponCode = null; await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<SavedCartDto> SaveCartAsync(Guid userId, string? name, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("السلة غير موجودة");
        if (!cart.Items.Any(i => !i.IsSavedForLater)) throw ApiException.BadRequest("لا توجد أصناف لحفظها");
        cart.Status = CartStatus.Saved; cart.SavedAt = DateTime.UtcNow;
        cart.Name = Clean(name, 100) ?? $"سلة {cart.SavedAt:dd/MM/yyyy HH:mm}";
        await db.SaveChangesAsync(ct);
        return new(cart.Id, cart.Name, cart.SavedAt.Value, cart.Items.Count(i => !i.IsSavedForLater),
            cart.Items.Where(i => !i.IsSavedForLater).Sum(i => i.CustomLineTotal ?? (i.PriceAtAdded ?? 0) * i.Quantity));
    }

    public Task<List<SavedCartDto>> SavedCartsAsync(Guid userId, CancellationToken ct = default) => db.Carts.AsNoTracking()
        .Where(c => c.UserId == userId && c.Status == CartStatus.Saved).OrderByDescending(c => c.SavedAt)
        .Select(c => new SavedCartDto(c.Id, c.Name ?? "سلة محفوظة", c.SavedAt ?? c.UpdatedAt ?? c.CreatedAt,
            c.Items.Count(i => !i.IsSavedForLater), c.Items.Where(i => !i.IsSavedForLater)
                .Sum(i => i.CustomLineTotal ?? (i.PriceAtAdded ?? 0) * i.Quantity))).ToListAsync(ct);

    public async Task<CartDto> RestoreCartAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var saved = await db.Carts.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && c.Status == CartStatus.Saved, ct)
            ?? throw ApiException.NotFound("السلة المحفوظة غير موجودة");
        var active = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct);
        if (active is not null)
        {
            active.Status = active.Items.Count == 0 ? CartStatus.Abandoned : CartStatus.Saved;
            active.SavedAt = active.Status == CartStatus.Saved ? DateTime.UtcNow : null;
            active.Name ??= active.Status == CartStatus.Saved ? $"سلة تلقائية {DateTime.UtcNow:dd/MM/yyyy HH:mm}" : null;
        }
        saved.Status = CartStatus.Active; saved.SavedAt = null; await db.SaveChangesAsync(ct);
        return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
    }

    public async Task<CartDto> AcknowledgePricesAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.PriceTiers)
            .Include(c => c.Items).ThenInclude(i => i.Variant)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active, ct)
            ?? throw ApiException.NotFound("السلة غير موجودة");
        foreach (var item in cart.Items)
            item.PriceAtAdded = item.CustomUnitPrice ?? await CurrentUnitPriceAsync(item.Product, item.Variant, item.Quantity, ct);
        await db.SaveChangesAsync(ct); return await MapAsync((await LoadCartAsync(userId, ct))!, ct);
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
            var previous = item.PriceAtAdded; var changed = previous is not null && Math.Abs(previous.Value - unitPrice) > .01m;
            return new(item.Id, product.Id, product.Slug, item.Variant?.Sku ?? product.Sku, product.NameAr, item.Variant?.NameAr,
                item.Quantity, product.MinOrderQty, available, unitPrice, line, Math.Max(0, before - line), product.Unit.NameAr,
                available <= 0 ? StockStatus.OutOfStock.ToString() : available <= product.LowStockThreshold ? StockStatus.LowStock.ToString() : StockStatus.InStock.ToString(),
                image, item.IsSavedForLater, item.CustomProductRequestId, item.CustomerNote, previous, changed, item.Quantity > available);
        }
        var mapped = cart.Items.Select(Map).ToList();
        var active = mapped.Where(i => !i.IsSavedForLater).ToList();
        var beforeSavings = active.Sum(i => i.LineTotal + i.Savings);
        var productSavings = active.Sum(i => i.Savings); var productSubtotal = active.Sum(i => i.LineTotal);
        var couponDiscount = await CouponDiscountAsync(cart.CouponCode, productSubtotal, active, cart, ct);
        var savings = productSavings + couponDiscount; var subtotal = Math.Max(0, productSubtotal - couponDiscount);
        var shipping = productSubtotal == 0 || productSubtotal >= 2000 ? 0 : 150;
        var total = subtotal + shipping;
        var tax = active.Sum(i => i.LineTotal * cart.Items.First(x => x.Id == i.Id).Product.TaxRatePercent / (100 + cart.Items.First(x => x.Id == i.Id).Product.TaxRatePercent));
        if (productSubtotal > 0 && couponDiscount > 0) tax *= subtotal / productSubtotal;
        return new(cart.Id, active, mapped.Where(i => i.IsSavedForLater).ToList(), active.Count, active.Sum(i => i.Quantity),
            beforeSavings, savings, subtotal, decimal.Round(tax, 2), shipping, total, productSubtotal >= 2000,
            cart.CouponCode, couponDiscount, cart.OrderNote, active.Any(i => i.PriceChanged), active.Any(i => i.HasAvailabilityIssue));
    }

    private async Task<decimal> CurrentUnitPriceAsync(Product product, ProductVariant? variant, int quantity, CancellationToken ct)
    {
        var contract = await db.CompanyProductPrices.AsNoTracking().Where(p => p.ProductId == product.Id &&
            (p.ValidFrom == null || p.ValidFrom <= DateTime.UtcNow) && (p.ValidTo == null || p.ValidTo >= DateTime.UtcNow))
            .OrderByDescending(p => p.ValidFrom).Select(p => (decimal?)p.ContractPrice).FirstOrDefaultAsync(ct);
        var tier = product.PriceTiers.Where(t => t.MinQty <= quantity).OrderByDescending(t => t.MinQty).FirstOrDefault();
        return (contract ?? tier?.UnitPrice ?? product.BasePrice) + (variant?.PriceAdjustment ?? 0);
    }

    private async Task<decimal> CouponDiscountAsync(string? code, decimal subtotal, IReadOnlyList<CartItemDto> active, Cart cart, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return 0;
        var now = DateTime.UtcNow;
        var coupon = await db.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code && c.IsActive &&
            (c.StartsAt == null || c.StartsAt <= now) && (c.ExpiresAt == null || c.ExpiresAt >= now) &&
            (c.UsageLimit == null || c.UsedCount < c.UsageLimit) && subtotal >= c.MinimumSubtotal, ct);
        if (coupon is null) return 0;
        if (coupon.OncePerCompany && await db.Orders.AnyAsync(x => x.CouponCode == code && x.Status != OrderStatus.Cancelled, ct)) return 0;
        if (coupon.NewCustomersOnly && await db.Orders.AnyAsync(x => x.Status != OrderStatus.Cancelled, ct)) return 0;
        if (!coupon.CanCombine && active.Any(x => x.Savings > 0)) return 0;
        var categoryIds = ParseCategoryIds(coupon.ApplicableCategoryIds);
        var eligibleIds = cart.Items.Where(x => !x.IsSavedForLater && (categoryIds.Count == 0 || categoryIds.Contains(x.Product.CategoryId)))
            .Select(x => x.Id).ToHashSet();
        var eligibleSubtotal = active.Where(x => eligibleIds.Contains(x.Id) && (!coupon.ExcludeDiscountedProducts || x.Savings == 0)).Sum(x => x.LineTotal);
        if (eligibleSubtotal <= 0 || subtotal < coupon.MinimumSubtotal) return 0;
        var discount = coupon.DiscountType == CouponDiscountType.Percentage ? eligibleSubtotal * coupon.DiscountValue / 100 : coupon.DiscountValue;
        return Math.Min(eligibleSubtotal, coupon.MaximumDiscount is { } max ? Math.Min(discount, max) : discount);
    }

    private static HashSet<Guid> ParseCategoryIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<Guid[]>(json)?.ToHashSet() ?? []; }
        catch (JsonException) { return []; }
    }

    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static CartDto Empty() => new(null, [], [], 0, 0, 0, 0, 0, 0, 0, 0, false, null, 0, null, false, false);
}
