using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;
using Mohandseto.Api.Application.Customization;

namespace Mohandseto.Api.Application.Shopping;

public sealed class CheckoutService(AppDbContext db, ITenantProvider tenantProvider, CartService carts, CustomizationService? customization = null)
{
    public async Task<CheckoutOptionsDto> OptionsAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = TenantId(); var cart = await RequireCartAsync(userId, ct);
        var session = await SessionAsync(userId, cart.Id!.Value, ct);
        var branches = await db.CompanyBranches.AsNoTracking().OrderByDescending(b => b.IsMain).ThenBy(b => b.Name)
            .Select(b => new CheckoutBranchDto(b.Id, b.Name, JoinAddress(b.Governorate, b.City, b.AddressLine), b.Phone, b.IsMain)).ToListAsync(ct);
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);
        var availableCredit = (company?.CreditLimit ?? 0) - (company?.CreditUsed ?? 0);
        var payments = new List<CheckoutPaymentOptionDto>
        {
            new(nameof(PaymentMethod.CreditLine), "الشراء الآجل - الحد الائتماني", availableCredit >= cart.Total, availableCredit >= cart.Total ? null : "الرصيد الائتماني المتاح غير كافٍ"),
            new(nameof(PaymentMethod.BankTransfer), "تحويل بنكي", true, null),
            new(nameof(PaymentMethod.CashOnDelivery), "نقدي عند الاستلام", cart.Total <= 20000, cart.Total <= 20000 ? null : "الحد الأقصى للدفع النقدي 20,000 ج.م"),
            new(nameof(PaymentMethod.MonthlyInvoice), "فاتورة شهرية مجمعة", availableCredit >= cart.Total, availableCredit >= cart.Total ? null : "الخدمة تحتاج حدًا ائتمانيًا متاحًا"),
        };
        return new(session.Id, cart, branches, payments, session.BranchId, session.ReceiverName, session.ReceiverPhone,
            session.RequiredDate, session.TimeSlot, session.ShippingMethod.ToString(), session.PaymentMethod?.ToString(),
            session.PurchaseOrderNumber, session.InternalReference);
    }

    public async Task<CheckoutOptionsDto> DeliveryAsync(Guid userId, UpdateDeliveryDto dto, CancellationToken ct = default)
    {
        var cart = await RequireCartAsync(userId, ct); var session = await SessionAsync(userId, cart.Id!.Value, ct);
        if (!await db.CompanyBranches.AnyAsync(b => b.Id == dto.BranchId, ct)) throw ApiException.BadRequest("عنوان التوصيل غير صالح");
        if (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.ReceiverPhone)) throw ApiException.BadRequest("بيانات مسؤول الاستلام مطلوبة");
        if (dto.RequiredDate.Date < DateTime.UtcNow.Date.AddDays(1)) throw ApiException.BadRequest("تاريخ التوصيل يجب أن يكون من الغد أو بعده");
        if (!Enum.TryParse<ShippingMethod>(dto.ShippingMethod, true, out var shipping)) throw ApiException.BadRequest("طريقة الشحن غير صالحة");
        session.BranchId = dto.BranchId; session.ReceiverName = dto.ReceiverName.Trim(); session.ReceiverPhone = dto.ReceiverPhone.Trim();
        session.RequiredDate = dto.RequiredDate.Date; session.TimeSlot = dto.TimeSlot.Trim(); session.ShippingMethod = shipping;
        await db.SaveChangesAsync(ct); return await OptionsAsync(userId, ct);
    }

    public async Task<CheckoutOptionsDto> PaymentAsync(Guid userId, UpdatePaymentDto dto, CancellationToken ct = default)
    {
        var options = await OptionsAsync(userId, ct);
        if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var method)) throw ApiException.BadRequest("طريقة الدفع غير صالحة");
        var option = options.PaymentOptions.First(p => p.Code == method.ToString());
        if (!option.Enabled) throw ApiException.Conflict(option.Reason ?? "طريقة الدفع غير متاحة");
        var session = await db.CheckoutSessions.FirstAsync(s => s.Id == options.SessionId, ct);
        session.PaymentMethod = method; session.PurchaseOrderNumber = dto.PurchaseOrderNumber?.Trim(); session.InternalReference = dto.InternalReference?.Trim();
        session.Status = session.BranchId is not null ? CheckoutStatus.Ready : CheckoutStatus.Draft;
        await db.SaveChangesAsync(ct); return await OptionsAsync(userId, ct);
    }

    public async Task<CheckoutReviewDto> ReviewAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await RequireCartAsync(userId, ct);
        var session = await db.CheckoutSessions.AsNoTracking().Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CartId == cart.Id && s.Status != CheckoutStatus.Submitted && s.ExpiresAt > DateTime.UtcNow, ct)
            ?? throw ApiException.BadRequest("ابدأ بيانات إتمام الطلب أولًا");
        if (session.Branch is null || session.RequiredDate is null || session.PaymentMethod is null || string.IsNullOrWhiteSpace(session.ReceiverName) || string.IsNullOrWhiteSpace(session.ReceiverPhone))
            throw ApiException.BadRequest("بيانات التوصيل والدفع غير مكتملة");
        var shipping = ShippingCost(session.ShippingMethod, cart.Subtotal);
        var total = cart.Subtotal + shipping;
        return new(session.Id, cart.Items, session.Branch.Name, JoinAddress(session.Branch.Governorate, session.Branch.City, session.Branch.AddressLine),
            session.ReceiverName, session.ReceiverPhone, session.RequiredDate.Value, session.TimeSlot ?? string.Empty,
            session.ShippingMethod.ToString(), session.PaymentMethod.Value.ToString(), session.PurchaseOrderNumber,
            cart.Subtotal, cart.Savings, cart.TaxIncluded, shipping, total, RequiresApproval(total));
    }

    public async Task<OrderCreatedDto> SubmitAsync(Guid userId, bool acceptTerms, CancellationToken ct = default)
    {
        if (!acceptTerms) throw ApiException.BadRequest("يجب الموافقة على شروط الطلب");
        var review = await ReviewAsync(userId, ct); var tenantId = TenantId();
        var session = await db.CheckoutSessions.Include(s => s.Cart).ThenInclude(c => c.Items)
            .FirstAsync(s => s.Id == review.SessionId, ct);
        var itemData = session.Cart.Items.Where(i => !i.IsSavedForLater).ToDictionary(i => i.Id);
        var order = new Order
        {
            TenantId = tenantId, Number = $"ORD-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}", UserId = userId,
            BranchId = session.BranchId!.Value, BranchName = review.BranchName, DeliveryAddress = review.DeliveryAddress,
            ReceiverName = review.ReceiverName, ReceiverPhone = review.ReceiverPhone, RequiredDate = review.RequiredDate,
            TimeSlot = review.TimeSlot, ShippingMethod = session.ShippingMethod, PaymentMethod = session.PaymentMethod!.Value,
            PurchaseOrderNumber = session.PurchaseOrderNumber, InternalReference = session.InternalReference,
            RequiresApproval = review.RequiresApproval, Status = review.RequiresApproval ? OrderStatus.PendingApproval : OrderStatus.Confirmed,
            Subtotal = review.Subtotal, Savings = review.Savings, TaxIncluded = review.TaxIncluded, Shipping = review.Shipping, Total = review.Total,
        };
        foreach (var item in review.Items)
        {
            var source = itemData[item.Id];
            order.Items.Add(new OrderItem { TenantId = tenantId, ProductId = item.ProductId, VariantId = source.VariantId, Sku = item.Sku,
                NameAr = item.NameAr, VariantName = item.VariantName, Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal, CustomizationJson = source.CustomizationJson });
        }
        order.History.Add(new OrderStatusHistory { TenantId = tenantId, Status = order.Status, ChangedBy = userId,
            Note = order.RequiresApproval ? "أرسل الطلب للموافقة الداخلية" : "تم تأكيد الطلب" });
        db.Orders.Add(order); session.Status = CheckoutStatus.Submitted; session.Cart.Status = CartStatus.Converted;
        var customRequestIds = session.Cart.Items.Where(i => !i.IsSavedForLater && i.CustomProductRequestId != null)
            .Select(i => i.CustomProductRequestId!.Value).ToList();
        if (customRequestIds.Count > 0)
            await (customization ?? throw new InvalidOperationException("CustomizationService is required for custom checkout"))
                .StartForOrderAsync(customRequestIds, order.Id, order.RequiresApproval, ct);
        await db.SaveChangesAsync(ct);
        return new(order.Id, order.Number, order.Status.ToString(), order.RequiresApproval, order.Total, order.RequiredDate);
    }

    private async Task<CartDto> RequireCartAsync(Guid userId, CancellationToken ct)
    {
        var cart = await carts.GetAsync(userId, ct);
        if (cart.Id is null || cart.Items.Count == 0) throw ApiException.BadRequest("السلة فارغة");
        return cart;
    }
    private async Task<CheckoutSession> SessionAsync(Guid userId, Guid cartId, CancellationToken ct)
    {
        var session = await db.CheckoutSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.CartId == cartId && s.Status != CheckoutStatus.Submitted && s.ExpiresAt > DateTime.UtcNow, ct);
        if (session is not null) return session;
        session = new CheckoutSession { TenantId = TenantId(), UserId = userId, CartId = cartId };
        db.CheckoutSessions.Add(session); await db.SaveChangesAsync(ct); return session;
    }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static decimal ShippingCost(ShippingMethod method, decimal subtotal) => method switch { ShippingMethod.Express => 150, ShippingMethod.Pickup => 0, _ => subtotal >= 2000 ? 0 : 150 };
    private static bool RequiresApproval(decimal total) => total >= 5000;
    private static string JoinAddress(params string?[] parts) => string.Join(" - ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
}
