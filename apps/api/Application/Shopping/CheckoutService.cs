using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Application.Approvals;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Shopping;

public sealed class CheckoutService(AppDbContext db, ITenantProvider tenantProvider, CartService carts,
    PaymentGatewayService payments, IWebHostEnvironment environment, CustomizationService? customization = null,
    IConfiguration? configuration = null, ApprovalService? approvals = null, FinanceService? finance = null)
{
    private static readonly Dictionary<string, string> AllowedAttachments = new(StringComparer.OrdinalIgnoreCase)
    { ["application/pdf"] = ".pdf", ["image/png"] = ".png", ["image/jpeg"] = ".jpg" };

    public async Task<CheckoutOptionsDto> OptionsAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = TenantId(); var cart = await RequireCartAsync(userId, ct);
        var session = await SessionAsync(userId, cart.Id!.Value, ct);
        var branches = await db.CompanyBranches.AsNoTracking().OrderByDescending(b => b.IsMain).ThenBy(b => b.Name)
            .Select(b => new CheckoutBranchDto(b.Id, b.Name, JoinAddress(b.Governorate, b.City, b.AddressLine), b.Phone,
                b.IsMain, b.Latitude, b.Longitude)).ToListAsync(ct);
        var centers = await db.CostCenters.AsNoTracking().Where(c => c.IsActive && c.PeriodStart <= DateTime.UtcNow && c.PeriodEnd >= DateTime.UtcNow)
            .OrderBy(c => c.Code).Select(c => new CheckoutCostCenterDto(c.Id, c.Code, c.NameAr, c.BudgetAmount,
                c.UsedAmount, c.ReservedAmount, c.BudgetAmount - c.UsedAmount - c.ReservedAmount, c.ApprovalThreshold)).ToListAsync(ct);
        var projects = await db.CompanyProjects.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Code)
            .Select(p => new CheckoutProjectDto(p.Id, p.Code, p.NameAr)).ToListAsync(ct);
        var receivers = await db.Users.AsNoTracking().Where(u => u.TenantId == tenantId && u.IsActive && u.PhoneVerified)
            .OrderBy(u => u.FullName).Select(u => new CheckoutReceiverDto(u.Id, u.FullName, u.Phone)).ToListAsync(ct);
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);
        var availableCredit = (company?.CreditLimit ?? 0) - (company?.CreditUsed ?? 0);
        var total = TotalFor(cart, session.ShippingMethod);
        var bank = BankInstructions();
        var bankConfigured = environment.IsDevelopment() || !string.IsNullOrWhiteSpace(configuration?["Payments:BankIban"]);
        var paymentOptions = new List<CheckoutPaymentOptionDto>
        {
            new(nameof(PaymentMethod.CreditLine), "الشراء الآجل - الحد الائتماني", availableCredit >= total,
                availableCredit >= total ? null : "الرصيد الائتماني المتاح غير كافٍ"),
            new(nameof(PaymentMethod.BankTransfer), "تحويل بنكي", bankConfigured,
                bankConfigured ? null : "بيانات الحساب البنكي غير مهيأة"),
            new(nameof(PaymentMethod.CashOnDelivery), "نقدي عند الاستلام", total <= 20000,
                total <= 20000 ? null : "الحد الأقصى للدفع النقدي 20,000 ج.م"),
            new(nameof(PaymentMethod.MonthlyInvoice), "فاتورة شهرية مجمعة", availableCredit >= total,
                availableCredit >= total ? null : "الخدمة تحتاج حدًا ائتمانيًا متاحًا"),
            new(nameof(PaymentMethod.Card), "الدفع الإلكتروني", payments.IsAvailable,
                payments.IsAvailable ? null : "بوابة الدفع الإلكتروني غير مهيأة"),
            new(nameof(PaymentMethod.Partial), "دفع جزئي بالحد الائتماني والبطاقة", payments.IsAvailable && availableCredit > 0 && availableCredit < total,
                payments.IsAvailable ? "يتاح عندما يغطي الحد الائتماني جزءًا من الإجمالي" : "بوابة الدفع الإلكتروني غير مهيأة"),
        };
        var attachments = await db.CheckoutAttachments.AsNoTracking().Where(a => a.CheckoutSessionId == session.Id)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        var purchaseOrder = attachments.FirstOrDefault(a => a.Type == CheckoutAttachmentType.PurchaseOrder);
        var bankReceipt = attachments.FirstOrDefault(a => a.Type == CheckoutAttachmentType.BankTransferReceipt);
        return new(session.Id, cart, branches, paymentOptions, centers, projects, receivers, session.BranchId,
            session.CostCenterId, session.ProjectId, session.RequestingDepartment, session.OrderNote,
            session.AllowSplitDelivery, session.ReceiverName, session.ReceiverPhone, session.RequiredDate,
            session.TimeSlot, session.ShippingMethod.ToString(), session.PaymentMethod?.ToString(),
            session.PurchaseOrderNumber, session.InternalReference, session.PaymentAttemptId, session.CreditPortion,
            session.CardPortion, purchaseOrder is null ? null : Attachment(purchaseOrder), bank,
            bankReceipt is null ? null : Attachment(bankReceipt));
    }

    public async Task<CheckoutOptionsDto> AddAddressAsync(Guid userId, CreateCheckoutAddressDto dto, CancellationToken ct = default)
    {
        _ = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw ApiException.Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Governorate) ||
            string.IsNullOrWhiteSpace(dto.City) || string.IsNullOrWhiteSpace(dto.AddressLine))
            throw ApiException.BadRequest("بيانات العنوان غير مكتملة");
        if (dto.Latitude is < -90 or > 90 || dto.Longitude is < -180 or > 180)
            throw ApiException.BadRequest("إحداثيات الموقع غير صالحة");
        var company = await db.Companies.FirstOrDefaultAsync(c => c.TenantId == TenantId(), ct)
            ?? throw ApiException.NotFound("الشركة غير موجودة");
        if (dto.IsMain)
            foreach (var branch in await db.CompanyBranches.Where(b => b.IsMain).ToListAsync(ct)) branch.IsMain = false;
        db.CompanyBranches.Add(new CompanyBranch { TenantId = TenantId(), CompanyId = company.Id, Name = CleanRequired(dto.Name, 120),
            Governorate = CleanRequired(dto.Governorate, 100), City = CleanRequired(dto.City, 100), AddressLine = CleanRequired(dto.AddressLine, 300),
            Phone = Clean(dto.Phone, 30), Latitude = dto.Latitude, Longitude = dto.Longitude, IsMain = dto.IsMain });
        await db.SaveChangesAsync(ct); return await OptionsAsync(userId, ct);
    }

    public async Task<CheckoutOptionsDto> ContextAsync(Guid userId, UpdateCheckoutContextDto dto, CancellationToken ct = default)
    {
        var cart = await RequireCartAsync(userId, ct); var session = await SessionAsync(userId, cart.Id!.Value, ct);
        if (!await db.CostCenters.AnyAsync(c => c.Id == dto.CostCenterId && c.IsActive, ct)) throw ApiException.BadRequest("مركز التكلفة غير صالح");
        if (dto.ProjectId is { } projectId && !await db.CompanyProjects.AnyAsync(p => p.Id == projectId && p.IsActive, ct))
            throw ApiException.BadRequest("المشروع غير صالح");
        session.CostCenterId = dto.CostCenterId; session.ProjectId = dto.ProjectId;
        session.RequestingDepartment = CleanRequired(dto.RequestingDepartment, 150);
        session.OrderNote = Clean(dto.OrderNote, 1500); session.InternalReference = Clean(dto.InternalReference, 150);
        await db.SaveChangesAsync(ct); return await OptionsAsync(userId, ct);
    }

    public async Task<CheckoutOptionsDto> DeliveryAsync(Guid userId, UpdateDeliveryDto dto, CancellationToken ct = default)
    {
        var cart = await RequireCartAsync(userId, ct); var session = await SessionAsync(userId, cart.Id!.Value, ct);
        if (!await db.CompanyBranches.AnyAsync(b => b.Id == dto.BranchId, ct)) throw ApiException.BadRequest("عنوان التوصيل غير صالح");
        if (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.ReceiverPhone)) throw ApiException.BadRequest("بيانات مسؤول الاستلام مطلوبة");
        if (dto.RequiredDate.Date < DateTime.UtcNow.Date.AddDays(1) || dto.RequiredDate.Date > DateTime.UtcNow.Date.AddDays(90))
            throw ApiException.BadRequest("تاريخ التوصيل يجب أن يكون بين الغد و90 يومًا");
        if (!Enum.TryParse<ShippingMethod>(dto.ShippingMethod, true, out var shipping)) throw ApiException.BadRequest("طريقة الشحن غير صالحة");
        session.BranchId = dto.BranchId; session.ReceiverName = CleanRequired(dto.ReceiverName, 150); session.ReceiverPhone = CleanRequired(dto.ReceiverPhone, 30);
        session.RequiredDate = dto.RequiredDate.Date; session.TimeSlot = CleanRequired(dto.TimeSlot, 40);
        session.ShippingMethod = shipping; session.AllowSplitDelivery = dto.AllowSplitDelivery;
        await db.SaveChangesAsync(ct); return await OptionsAsync(userId, ct);
    }

    public async Task<CheckoutOptionsDto> PaymentAsync(Guid userId, UpdatePaymentDto dto, CancellationToken ct = default)
    {
        var options = await OptionsAsync(userId, ct);
        if (!Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var method)) throw ApiException.BadRequest("طريقة الدفع غير صالحة");
        var option = options.PaymentOptions.First(p => p.Code == method.ToString());
        if (!option.Enabled) throw ApiException.Conflict(option.Reason ?? "طريقة الدفع غير متاحة");
        var session = await db.CheckoutSessions.FirstAsync(s => s.Id == options.SessionId, ct);
        var total = TotalFor(options.Cart, session.ShippingMethod);
        if (method == PaymentMethod.Card)
        {
            if (dto.PaymentAttemptId is not { } attemptId) throw ApiException.BadRequest("عملية الدفع الإلكتروني مطلوبة");
            await payments.RequireSucceededAsync(userId, attemptId, total, ct);
            session.PaymentAttemptId = attemptId; session.CardPortion = total; session.CreditPortion = 0;
        }
        else if (method == PaymentMethod.Partial)
        {
            if (dto.PaymentAttemptId is not { } attemptId || dto.CreditPortion is not > 0 || dto.CardPortion is not > 0 || dto.CreditPortion + dto.CardPortion != total)
                throw ApiException.BadRequest("يجب توزيع الإجمالي كاملًا بين الحد الائتماني والبطاقة");
            var company = await db.Companies.AsNoTracking().FirstAsync(c => c.TenantId == TenantId(), ct);
            if (dto.CreditPortion > company.CreditLimit - company.CreditUsed) throw ApiException.Conflict("جزء الحد الائتماني يتجاوز المتاح");
            await payments.RequireSucceededAsync(userId, attemptId, dto.CardPortion.Value, ct);
            session.PaymentAttemptId = attemptId; session.CreditPortion = dto.CreditPortion; session.CardPortion = dto.CardPortion;
        }
        else { session.PaymentAttemptId = null; session.CardPortion = null; session.CreditPortion = null; }
        session.PaymentMethod = method; session.PurchaseOrderNumber = Clean(dto.PurchaseOrderNumber, 100);
        session.InternalReference = Clean(dto.InternalReference, 150) ?? session.InternalReference;
        session.Status = session.BranchId is not null && session.CostCenterId is not null ? CheckoutStatus.Ready : CheckoutStatus.Draft;
        await db.SaveChangesAsync(ct); return await OptionsAsync(userId, ct);
    }

    public async Task<CheckoutAttachmentDto> UploadAttachmentAsync(Guid userId, IFormFile file,
        CheckoutAttachmentType type = CheckoutAttachmentType.PurchaseOrder, CancellationToken ct = default)
    {
        var cart = await RequireCartAsync(userId, ct); var session = await SessionAsync(userId, cart.Id!.Value, ct);
        if (file.Length is <= 0 or > 10 * 1024 * 1024 || !AllowedAttachments.TryGetValue(file.ContentType, out var ext))
            throw ApiException.BadRequest("مرفق أمر الشراء يجب أن يكون PDF أو PNG أو JPG وبحجم أقصى 10 ميجابايت");
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "checkout", TenantId().ToString("N"), session.Id.ToString("N"));
        Directory.CreateDirectory(folder); var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}");
        await using (var stream = File.Create(absolute)) await file.CopyToAsync(stream, ct);
        var old = await db.CheckoutAttachments.Where(a => a.CheckoutSessionId == session.Id && a.Type == type).ToListAsync(ct);
        if (old.Count > 0) db.CheckoutAttachments.RemoveRange(old);
        var attachment = new CheckoutAttachment { TenantId = TenantId(), CheckoutSessionId = session.Id,
            Type = type, OriginalName = Path.GetFileName(file.FileName),
            StoredPath = Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/'), ContentType = file.ContentType, SizeBytes = file.Length };
        db.CheckoutAttachments.Add(attachment); await db.SaveChangesAsync(ct); return Attachment(attachment);
    }

    public async Task<(string Path, string ContentType, string Name)> AttachmentAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var file = await db.CheckoutAttachments.AsNoTracking().Include(a => a.CheckoutSession)
            .FirstOrDefaultAsync(a => a.Id == id && a.CheckoutSession.UserId == userId, ct) ?? throw ApiException.NotFound("المرفق غير موجود");
        return (SafeStoredPath(file.StoredPath), file.ContentType, file.OriginalName);
    }

    public async Task<PaymentAttemptDto> CreatePaymentAsync(Guid userId, CreatePaymentAttemptDto dto, CancellationToken ct = default)
    {
        var options = await OptionsAsync(userId, ct);
        if (dto.Amount > TotalFor(options.Cart, Enum.Parse<ShippingMethod>(options.ShippingMethod)))
            throw ApiException.BadRequest("مبلغ الدفع يتجاوز إجمالي الطلب");
        return await payments.CreateAsync(userId, options.SessionId, dto.IdempotencyKey, dto.Amount, ct);
    }
    public Task<PaymentAttemptDto> ConfirmPaymentAsync(Guid userId, Guid id, ConfirmPaymentAttemptDto dto, CancellationToken ct = default) =>
        payments.ConfirmAsync(userId, id, dto.PaymentToken, ct);

    public async Task<CheckoutReviewDto> ReviewAsync(Guid userId, CancellationToken ct = default)
    {
        var cart = await RequireCartAsync(userId, ct);
        var session = await db.CheckoutSessions.AsNoTracking().Include(s => s.Branch)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CartId == cart.Id && s.Status != CheckoutStatus.Submitted && s.ExpiresAt > DateTime.UtcNow, ct)
            ?? throw ApiException.BadRequest("ابدأ بيانات إتمام الطلب أولًا");
        if (session.Branch is null || session.RequiredDate is null || session.PaymentMethod is null || session.CostCenterId is null ||
            string.IsNullOrWhiteSpace(session.ReceiverName) || string.IsNullOrWhiteSpace(session.ReceiverPhone) || string.IsNullOrWhiteSpace(session.RequestingDepartment))
            throw ApiException.BadRequest("بيانات الجهة الطالبة والتوصيل والدفع غير مكتملة");
        var center = await db.CostCenters.AsNoTracking().FirstAsync(c => c.Id == session.CostCenterId, ct);
        var project = session.ProjectId is null ? null : await db.CompanyProjects.AsNoTracking().FirstAsync(p => p.Id == session.ProjectId, ct);
        var attachments = await db.CheckoutAttachments.AsNoTracking().Where(a => a.CheckoutSessionId == session.Id)
            .OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        var attachment = attachments.FirstOrDefault(a => a.Type == CheckoutAttachmentType.PurchaseOrder);
        var bankReceipt = attachments.FirstOrDefault(a => a.Type == CheckoutAttachmentType.BankTransferReceipt);
        var shipping = ShippingCost(session.ShippingMethod, cart.Subtotal); var total = cart.Subtotal + shipping;
        var available = center.BudgetAmount - center.UsedAmount - center.ReservedAmount; var exceeded = total > available;
        var requiresApproval = exceeded || total >= (center.ApprovalThreshold ?? 5000);
        return new(session.Id, cart.Items, session.Branch.Name, JoinAddress(session.Branch.Governorate, session.Branch.City, session.Branch.AddressLine),
            session.ReceiverName, session.ReceiverPhone, session.RequiredDate.Value, session.TimeSlot ?? string.Empty,
            session.ShippingMethod.ToString(), session.PaymentMethod.Value.ToString(), session.PurchaseOrderNumber,
            center.Code, center.NameAr, project?.NameAr, session.RequestingDepartment, session.OrderNote, session.AllowSplitDelivery,
            attachment is null ? null : Attachment(attachment), bankReceipt is null ? null : Attachment(bankReceipt),
            cart.Subtotal, cart.Savings, cart.CouponCode, cart.CouponDiscount, cart.TaxIncluded, shipping,
            total, available, exceeded, requiresApproval);
    }

    public async Task<OrderCreatedDto> SubmitAsync(Guid userId, bool acceptTerms, CancellationToken ct = default)
    {
        if (!acceptTerms) throw ApiException.BadRequest("يجب الموافقة على شروط الطلب");
        var review = await ReviewAsync(userId, ct); var tenantId = TenantId();
        var session = await db.CheckoutSessions.Include(s => s.Cart).ThenInclude(c => c.Items).FirstAsync(s => s.Id == review.SessionId, ct);
        var center = await db.CostCenters.FirstAsync(c => c.Id == session.CostCenterId, ct);
        var project = session.ProjectId is null ? null : await db.CompanyProjects.FirstAsync(p => p.Id == session.ProjectId, ct);
        var itemData = session.Cart.Items.Where(i => !i.IsSavedForLater).ToDictionary(i => i.Id);
        var order = new Order
        {
            TenantId = tenantId, Number = $"ORD-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}", UserId = userId,
            BranchId = session.BranchId!.Value, BranchName = review.BranchName, DeliveryAddress = review.DeliveryAddress,
            ReceiverName = review.ReceiverName, ReceiverPhone = review.ReceiverPhone, RequiredDate = review.RequiredDate,
            TimeSlot = review.TimeSlot, ShippingMethod = session.ShippingMethod, PaymentMethod = session.PaymentMethod!.Value,
            PurchaseOrderNumber = session.PurchaseOrderNumber, InternalReference = session.InternalReference,
            CostCenterId = center.Id, CostCenterCode = center.Code, CostCenterName = center.NameAr,
            ProjectId = project?.Id, ProjectCode = project?.Code, ProjectName = project?.NameAr,
            RequestingDepartment = session.RequestingDepartment, OrderNote = session.OrderNote, AllowSplitDelivery = session.AllowSplitDelivery,
            PaymentAttemptId = session.PaymentAttemptId, CreditPortion = session.CreditPortion, CardPortion = session.CardPortion,
            RequiresApproval = review.RequiresApproval, Status = review.RequiresApproval ? OrderStatus.PendingApproval : OrderStatus.Confirmed,
            Subtotal = review.Subtotal, Savings = review.Savings, CouponCode = review.CouponCode,
            CouponDiscount = review.CouponDiscount, TaxIncluded = review.TaxIncluded, Shipping = review.Shipping, Total = review.Total,
        };
        foreach (var item in review.Items)
        {
            var source = itemData[item.Id];
            order.Items.Add(new OrderItem { TenantId = tenantId, ProductId = item.ProductId, VariantId = source.VariantId, Sku = item.Sku,
                NameAr = item.NameAr, VariantName = item.VariantName, Quantity = item.Quantity, UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal, CustomizationJson = source.CustomizationJson, CustomerNote = source.CustomerNote });
        }
        order.History.Add(new OrderStatusHistory { TenantId = tenantId, Status = order.Status, ChangedBy = userId,
            Note = order.RequiresApproval ? "أرسل الطلب للموافقة الداخلية" : "تم تأكيد الطلب" });
        db.Orders.Add(order); session.Status = CheckoutStatus.Submitted; session.Cart.Status = CartStatus.Converted;
        finance?.IssueForOrder(order);
        if (order.RequiresApproval) center.ReservedAmount += order.Total; else center.UsedAmount += order.Total;
        if (order.RequiresApproval && approvals is not null)
            await approvals.CreateForOrderAsync(order, review.BudgetExceeded, userId, ct);
        if (!string.IsNullOrWhiteSpace(order.CouponCode))
        {
            var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == order.CouponCode, ct);
            if (coupon is not null) coupon.UsedCount++;
        }
        var attachments = await db.CheckoutAttachments.Where(a => a.CheckoutSessionId == session.Id).ToListAsync(ct);
        foreach (var attachment in attachments) attachment.OrderId = order.Id;
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
        if (cart.HasPriceChanges) throw ApiException.Conflict("تغير سعر صنف أو أكثر؛ راجع الأسعار وأكّدها أولًا");
        if (cart.HasAvailabilityIssues) throw ApiException.Conflict("بعض الكميات لم تعد متاحة؛ عدّل السلة أولًا");
        return cart;
    }
    private async Task<CheckoutSession> SessionAsync(Guid userId, Guid cartId, CancellationToken ct)
    {
        var session = await db.CheckoutSessions.FirstOrDefaultAsync(s => s.UserId == userId && s.CartId == cartId && s.Status != CheckoutStatus.Submitted && s.ExpiresAt > DateTime.UtcNow, ct);
        if (session is not null) return session;
        session = new CheckoutSession { TenantId = TenantId(), UserId = userId, CartId = cartId };
        db.CheckoutSessions.Add(session); await db.SaveChangesAsync(ct); return session;
    }
    private string SafeStoredPath(string relative)
    {
        var root = Path.GetFullPath(environment.ContentRootPath) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw ApiException.NotFound("المرفق غير موجود");
        return path;
    }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static decimal ShippingCost(ShippingMethod method, decimal subtotal) => method switch { ShippingMethod.Express => 150, ShippingMethod.Pickup => 0, _ => subtotal >= 2000 ? 0 : 150 };
    private static decimal TotalFor(CartDto cart, ShippingMethod method) => cart.Subtotal + ShippingCost(method, cart.Subtotal);
    private static CheckoutAttachmentDto Attachment(CheckoutAttachment a) => new(a.Id, a.OriginalName, a.ContentType, a.SizeBytes, $"/api/checkout/attachments/{a.Id}");
    private BankTransferInstructionsDto BankInstructions() => new(
        configuration?["Payments:BankName"] ?? "بنك التطوير التجريبي",
        configuration?["Payments:BankAccountName"] ?? "شركة مهندسيتو توريدات",
        configuration?["Payments:BankIban"] ?? "EG00 0000 0000 0000 0000 0000 000",
        "EGP");
    private static string JoinAddress(params string?[] parts) => string.Join(" - ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    private static string CleanRequired(string? value, int max) => string.IsNullOrWhiteSpace(value) ? throw ApiException.BadRequest("البيان المطلوب غير مكتمل") : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
