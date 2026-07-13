using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Shopping;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Orders;

public sealed class OrderService(AppDbContext db, IWebHostEnvironment environment,
    CartService cart)
{
    private static readonly HashSet<string> AllowedFileTypes = ["image/jpeg", "image/png", "application/pdf"];
    private static readonly OrderStatus[] Trackable = [OrderStatus.Picking, OrderStatus.Packing, OrderStatus.Shipped,
        OrderStatus.OutForDelivery, OrderStatus.PartiallyDelivered, OrderStatus.Delivered, OrderStatus.Delayed];

    public async Task<List<OrderListDto>> ListAsync(Guid userId, string? search, string? status,
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(o => o.Number.Contains(term)); }
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsed)) query = query.Where(o => o.Status == parsed);
        if (from is not null) query = query.Where(o => o.CreatedAt >= from.Value.Date);
        if (to is not null) query = query.Where(o => o.CreatedAt < to.Value.Date.AddDays(1));
        return await query.OrderByDescending(o => o.CreatedAt).Select(o => new OrderListDto(o.Id, o.Number,
            o.Status.ToString(), o.Total, o.Items.Count, o.RequiredDate, o.CreatedAt,
            o.Status != OrderStatus.Cancelled && o.Status < OrderStatus.Shipped,
            Trackable.Contains(o.Status))).ToListAsync(ct);
    }

    public async Task<OrderDetailDto> DetailAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.History)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw ApiException.NotFound("الطلب غير موجود");
        var shipments = await db.Shipments.AsNoTracking().Include(s => s.Events).Where(s => s.OrderId == order.Id)
            .OrderBy(s => s.CreatedAt).ToListAsync(ct);
        var proofs = await db.DeliveryProofs.AsNoTracking().Where(p => p.OrderId == order.Id).OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        var issues = await db.OrderIssues.AsNoTracking().Where(i => i.OrderId == order.Id && i.UserId == userId).OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        var rating = await db.OrderRatings.AsNoTracking().FirstOrDefaultAsync(r => r.OrderId == order.Id && r.UserId == userId, ct);
        var itemRatings = await db.OrderItemRatings.AsNoTracking().Where(r => r.UserId == userId && order.Items.Select(i => i.Id).Contains(r.OrderItemId))
            .ToDictionaryAsync(r => r.OrderItemId, r => r.Rating, ct);
        var schedules = await db.RecurringOrderSchedules.AsNoTracking().Where(s => s.SourceOrderId == order.Id && s.UserId == userId).ToListAsync(ct);
        return new OrderDetailDto(order.Id, order.Number, order.Status.ToString(), order.Subtotal, order.Savings,
            order.CouponDiscount, order.TaxIncluded, order.Shipping, order.Total, order.BranchName, order.DeliveryAddress,
            order.ReceiverName, order.ReceiverPhone, order.RequiredDate, order.TimeSlot, order.ShippingMethod.ToString(),
            order.PaymentMethod.ToString(), order.PurchaseOrderNumber, order.InternalReference, order.CostCenterCode,
            order.CostCenterName, order.ProjectCode, order.ProjectName, order.RequestingDepartment, order.OrderNote,
            order.AllowSplitDelivery, order.RequiresApproval, CanCancel(order.Status), Trackable.Contains(order.Status),
            order.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.VariantId, i.Sku, i.NameAr, i.VariantName,
                i.Quantity, i.UnitPrice, i.LineTotal, i.CustomerNote, itemRatings.GetValueOrDefault(i.Id))).ToList(),
            order.History.OrderBy(h => h.CreatedAt).Select(h => new OrderHistoryDto(h.Status.ToString(), h.Note, h.CreatedAt)).ToList(),
            shipments.Select(s => new ShipmentDto(s.Id, s.Number, s.CarrierName, s.TrackingNumber, s.Status,
                s.DriverName, s.DriverPhone, s.DriverLatitude, s.DriverLongitude, s.EstimatedArrival, s.DeliveredAt,
                s.Events.OrderBy(e => e.CreatedAt).Select(e => new ShipmentEventDto(e.Id, e.Status, e.DescriptionAr,
                    e.Location, e.Latitude, e.Longitude, e.CreatedAt)).ToList())).ToList(),
            proofs.Select(p => new DeliveryProofDto(p.Id, p.Type.ToString(), p.RecipientName, p.Note, p.CreatedAt, p.StoredPath is not null)).ToList(),
            issues.Select(i => new OrderIssueDto(i.Id, i.OrderItemId, i.Type.ToString(), i.AffectedQuantity,
                i.Description, i.Status.ToString(), i.CreatedAt, i.StoredPhotoPath is not null)).ToList(),
            rating is null ? null : new OrderRatingDto(rating.DeliveryRating, rating.ServiceRating, rating.Comment),
            schedules.Select(s => new RecurringScheduleDto(s.Id, s.Frequency, s.Interval, s.NextRunAt,
                s.EndsAt, s.IsActive, s.RequireApprovalEachRun)).ToList());
    }

    public async Task<OrderDetailDto> CancelAsync(Guid userId, Guid orderId, CancelOrderDto dto, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct);
        if (!CanCancel(order.Status)) throw ApiException.Conflict("لا يمكن إلغاء الطلب بعد بدء الشحن");
        if (string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("سبب الإلغاء مطلوب");
        if (order.CostCenterId is { } centerId)
        {
            var center = await db.CostCenters.FirstOrDefaultAsync(c => c.Id == centerId, ct);
            if (center is not null)
            {
                if (order.Status == OrderStatus.PendingApproval) center.ReservedAmount = Math.Max(0, center.ReservedAmount - order.Total);
                else center.UsedAmount = Math.Max(0, center.UsedAmount - order.Total);
            }
        }
        db.OrderCancellations.Add(new OrderCancellation { TenantId = order.TenantId, OrderId = order.Id, UserId = userId,
            Reason = Clean(dto.Reason, 150)!, Details = Clean(dto.Details, 1000), CancelledAt = DateTime.UtcNow });
        SetStatus(order, OrderStatus.Cancelled, userId, $"إلغاء: {Clean(dto.Reason, 150)}");
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, orderId, ct);
    }

    public async Task<ReorderResultDto> ReorderAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct)
            ?? throw ApiException.NotFound("الطلب غير موجود");
        var added = 0; var skipped = 0; Guid? cartId = null;
        foreach (var item in order.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.CustomizationJson)) { skipped++; continue; }
            try { var result = await cart.AddAsync(userId, new AddCartItemDto(item.ProductId, item.VariantId, item.Quantity, null), ct); added++; cartId = result.Id; }
            catch (ApiException) { skipped++; }
        }
        if (added == 0) throw ApiException.Conflict("لا توجد منتجات متاحة لإعادة الطلب؛ المنتجات المخصصة تحتاج إعدادًا جديدًا");
        return new ReorderResultDto(added, skipped, cartId);
    }

    public async Task<RecurringScheduleDto> ScheduleAsync(Guid userId, Guid orderId, CreateRecurringScheduleDto dto, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct);
        var allowed = new[] { "Weekly", "Monthly", "Quarterly" };
        if (!allowed.Contains(dto.Frequency, StringComparer.OrdinalIgnoreCase) || dto.Interval is < 1 or > 12 || dto.NextRunAt <= DateTime.UtcNow || dto.EndsAt <= dto.NextRunAt)
            throw ApiException.BadRequest("بيانات الجدولة غير صالحة");
        var schedule = new RecurringOrderSchedule { TenantId = order.TenantId, SourceOrderId = order.Id, UserId = userId,
            Frequency = allowed.First(x => x.Equals(dto.Frequency, StringComparison.OrdinalIgnoreCase)), Interval = dto.Interval,
            NextRunAt = dto.NextRunAt, EndsAt = dto.EndsAt, RequireApprovalEachRun = dto.RequireApprovalEachRun };
        db.RecurringOrderSchedules.Add(schedule); await db.SaveChangesAsync(ct);
        return new(schedule.Id, schedule.Frequency, schedule.Interval, schedule.NextRunAt, schedule.EndsAt, schedule.IsActive, schedule.RequireApprovalEachRun);
    }

    public async Task<OrderIssueDto> ReportIssueAsync(Guid userId, Guid orderId, string type, Guid? orderItemId,
        int? quantity, string description, IFormFile? photo, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct);
        if (!Enum.TryParse<OrderIssueType>(type, true, out var issueType) || string.IsNullOrWhiteSpace(description))
            throw ApiException.BadRequest("بيانات البلاغ غير مكتملة");
        if (order.Status is not (OrderStatus.Shipped or OrderStatus.OutForDelivery or OrderStatus.PartiallyDelivered or OrderStatus.Delivered or OrderStatus.Completed))
            throw ApiException.Conflict("يمكن الإبلاغ عن مشكلة بعد شحن الطلب");
        if (orderItemId is not null && !await db.OrderItems.AnyAsync(i => i.Id == orderItemId && i.OrderId == order.Id, ct)) throw ApiException.BadRequest("صنف الطلب غير صالح");
        var issue = new OrderIssue { TenantId = order.TenantId, OrderId = order.Id, OrderItemId = orderItemId,
            UserId = userId, Type = issueType, AffectedQuantity = quantity, Description = Clean(description, 1500)! };
        if (photo is not null) issue.StoredPhotoPath = await SaveFileAsync(order, "issues", photo, ct);
        db.OrderIssues.Add(issue); db.Notifications.Add(new AppNotification { TenantId = order.TenantId, UserId = userId,
            Type = "order.issue", Title = "تم تسجيل بلاغ الطلب", Body = $"سنراجع بلاغك على {order.Number}", EntityType = nameof(Order), EntityId = order.Id });
        await db.SaveChangesAsync(ct); return new(issue.Id, issue.OrderItemId, issue.Type.ToString(), issue.AffectedQuantity,
            issue.Description, issue.Status.ToString(), issue.CreatedAt, issue.StoredPhotoPath is not null);
    }

    public async Task<OrderRatingDto> RateAsync(Guid userId, Guid orderId, RateOrderDto dto, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct); EnsureDelivered(order);
        if (!ValidRating(dto.DeliveryRating) || !ValidRating(dto.ServiceRating)) throw ApiException.BadRequest("التقييم يجب أن يكون من 1 إلى 5");
        var rating = await db.OrderRatings.FirstOrDefaultAsync(r => r.OrderId == order.Id && r.UserId == userId, ct);
        if (rating is null) { rating = new OrderRating { TenantId = order.TenantId, OrderId = order.Id, UserId = userId }; db.OrderRatings.Add(rating); }
        rating.DeliveryRating = dto.DeliveryRating; rating.ServiceRating = dto.ServiceRating; rating.Comment = Clean(dto.Comment, 1000);
        await db.SaveChangesAsync(ct); return new(rating.DeliveryRating, rating.ServiceRating, rating.Comment);
    }

    public async Task RateItemAsync(Guid userId, Guid orderId, Guid itemId, RateOrderItemDto dto, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct); EnsureDelivered(order);
        if (!ValidRating(dto.Rating) || !await db.OrderItems.AnyAsync(i => i.Id == itemId && i.OrderId == order.Id, ct)) throw ApiException.BadRequest("التقييم أو الصنف غير صالح");
        var rating = await db.OrderItemRatings.FirstOrDefaultAsync(r => r.OrderItemId == itemId && r.UserId == userId, ct);
        if (rating is null) { rating = new OrderItemRating { TenantId = order.TenantId, OrderItemId = itemId, UserId = userId }; db.OrderItemRatings.Add(rating); }
        rating.Rating = dto.Rating; rating.Comment = Clean(dto.Comment, 1000); await db.SaveChangesAsync(ct);
    }

    public async Task<RequestDeliveryOtpResultDto> RequestOtpAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct);
        if (order.Status is not (OrderStatus.OutForDelivery or OrderStatus.PartiallyDelivered)) throw ApiException.Conflict("رمز الاستلام متاح عند خروج الطلب للتوصيل");
        var code = Random.Shared.Next(100000, 999999).ToString();
        var confirmation = await db.DeliveryConfirmations.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(x => x.OrderId == order.Id && x.ConfirmedAt == null, ct);
        if (confirmation is null) { confirmation = new DeliveryConfirmation { TenantId = order.TenantId, OrderId = order.Id }; db.DeliveryConfirmations.Add(confirmation); }
        confirmation.CodeHash = BCrypt.Net.BCrypt.HashPassword(code); confirmation.ExpiresAt = DateTime.UtcNow.AddMinutes(10); confirmation.Attempts = 0;
        await db.SaveChangesAsync(ct);
        return new(confirmation.ExpiresAt, environment.IsDevelopment() ? code : null);
    }

    public async Task<OrderDetailDto> ConfirmOtpAsync(Guid userId, Guid orderId, ConfirmDeliveryOtpDto dto, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct);
        var confirmation = await db.DeliveryConfirmations.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(x => x.OrderId == order.Id && x.ConfirmedAt == null, ct)
            ?? throw ApiException.NotFound("اطلب رمز استلام جديدًا");
        if (confirmation.ExpiresAt < DateTime.UtcNow) throw ApiException.Conflict("انتهت صلاحية رمز الاستلام");
        if (confirmation.Attempts >= 5) throw ApiException.TooMany("تم تجاوز محاولات رمز الاستلام");
        if (!BCrypt.Net.BCrypt.Verify(dto.Code, confirmation.CodeHash)) { confirmation.Attempts++; await db.SaveChangesAsync(ct); throw ApiException.BadRequest("رمز الاستلام غير صحيح"); }
        confirmation.ConfirmedAt = DateTime.UtcNow;
        db.DeliveryProofs.Add(new DeliveryProof { TenantId = order.TenantId, OrderId = order.Id, Type = DeliveryProofType.Otp,
            RecipientName = Clean(dto.RecipientName, 150), Note = "تأكيد برمز الاستلام" });
        SetStatus(order, OrderStatus.Delivered, userId, "تم تأكيد الاستلام بالرمز"); MarkShipmentsDelivered(order.Id);
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, orderId, ct);
    }

    public async Task<DeliveryProofDto> UploadProofAsync(Guid userId, Guid orderId, string type, string? recipientName,
        string? note, IFormFile file, CancellationToken ct = default)
    {
        var order = await OwnedAsync(userId, orderId, ct);
        if (!Enum.TryParse<DeliveryProofType>(type, true, out var proofType) || proofType is DeliveryProofType.Otp)
            throw ApiException.BadRequest("نوع إثبات الاستلام غير صالح");
        if (order.Status is not (OrderStatus.OutForDelivery or OrderStatus.PartiallyDelivered or OrderStatus.Delivered)) throw ApiException.Conflict("الطلب ليس في مرحلة الاستلام");
        var path = await SaveFileAsync(order, "proofs", file, ct);
        var proof = new DeliveryProof { TenantId = order.TenantId, OrderId = order.Id, Type = proofType,
            RecipientName = Clean(recipientName, 150), Note = Clean(note, 500), StoredPath = path,
            OriginalName = Path.GetFileName(file.FileName), ContentType = file.ContentType };
        db.DeliveryProofs.Add(proof); SetStatus(order, OrderStatus.Delivered, userId, "تم رفع إثبات الاستلام"); MarkShipmentsDelivered(order.Id);
        await db.SaveChangesAsync(ct); return new(proof.Id, proof.Type.ToString(), proof.RecipientName, proof.Note, proof.CreatedAt, true);
    }

    public async Task<OrderDetailDto> FulfillAsync(Guid staffUserId, Guid orderId, FulfillmentUpdateDto dto, CancellationToken ct = default)
    {
        var order = await db.Orders.IgnoreQueryFilters().Include(o => o.History).FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, ct)
            ?? throw ApiException.NotFound("الطلب غير موجود");
        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var status) || status is OrderStatus.PendingApproval or OrderStatus.Cancelled)
            throw ApiException.BadRequest("حالة الطلب غير صالحة");
        if (!ValidTransition(order.Status, status)) throw ApiException.Conflict($"لا يمكن نقل الطلب من {order.Status} إلى {status}");
        Shipment? shipment = null;
        if (status >= OrderStatus.Picking && status <= OrderStatus.Delayed)
        {
            shipment = await db.Shipments.IgnoreQueryFilters().Include(s => s.Events).OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(s => s.OrderId == order.Id && !s.IsDeleted, ct);
            if (shipment is null)
            {
                shipment = new Shipment { TenantId = order.TenantId, OrderId = order.Id,
                    Number = Clean(dto.ShipmentNumber, 50) ?? $"SHP-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    CarrierName = Clean(dto.CarrierName, 100) ?? "Mohandseto Logistics" };
                db.Shipments.Add(shipment);
            }
            shipment.TrackingNumber = Clean(dto.TrackingNumber, 100) ?? shipment.TrackingNumber; shipment.Status = status.ToString();
            shipment.DriverName = Clean(dto.DriverName, 150) ?? shipment.DriverName; shipment.DriverPhone = Clean(dto.DriverPhone, 30) ?? shipment.DriverPhone;
            shipment.DriverLatitude = dto.Latitude ?? shipment.DriverLatitude; shipment.DriverLongitude = dto.Longitude ?? shipment.DriverLongitude;
            shipment.EstimatedArrival = dto.EstimatedArrival ?? shipment.EstimatedArrival;
            if (status is OrderStatus.Delivered or OrderStatus.Completed) shipment.DeliveredAt = DateTime.UtcNow;
            db.ShipmentEvents.Add(new ShipmentEvent { TenantId = order.TenantId, ShipmentId = shipment.Id,
                Status = status.ToString(), DescriptionAr = Clean(dto.Note, 500) ?? StatusLabel(status), Location = Clean(dto.Location, 200),
                Latitude = dto.Latitude, Longitude = dto.Longitude });
        }
        SetStatus(order, status, staffUserId, Clean(dto.Note, 500));
        db.Notifications.Add(new AppNotification { TenantId = order.TenantId, UserId = order.UserId, Type = "order.status",
            Title = "تحديث حالة الطلب", Body = $"{order.Number}: {StatusLabel(status)}", EntityType = nameof(Order), EntityId = order.Id });
        await db.SaveChangesAsync(ct); return await DetailAsync(order.UserId, order.Id, ct);
    }

    public async Task<(string Path, string Type, string Name)> ProofFileAsync(Guid userId, Guid proofId, CancellationToken ct = default)
    {
        var proof = await db.DeliveryProofs.AsNoTracking().Include(p => p.Order).FirstOrDefaultAsync(p => p.Id == proofId && p.Order.UserId == userId, ct)
            ?? throw ApiException.NotFound("إثبات الاستلام غير موجود");
        if (proof.StoredPath is null) throw ApiException.NotFound("لا يوجد ملف لهذا الإثبات");
        return (SafePath(proof.StoredPath), proof.ContentType ?? "application/octet-stream", proof.OriginalName ?? "proof");
    }

    private async Task<Order> OwnedAsync(Guid userId, Guid id, CancellationToken ct) => await db.Orders.Include(o => o.History)
        .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId, ct) ?? throw ApiException.NotFound("الطلب غير موجود");
    private static bool CanCancel(OrderStatus status) => status is OrderStatus.PendingApproval or OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.Picking or OrderStatus.Packing;
    private static bool ValidRating(int rating) => rating is >= 1 and <= 5;
    private static void EnsureDelivered(Order order) { if (order.Status is not (OrderStatus.Delivered or OrderStatus.Completed)) throw ApiException.Conflict("يمكن التقييم بعد استلام الطلب"); }
    private static bool ValidTransition(OrderStatus current, OrderStatus next) => next == current || next == OrderStatus.Delayed ||
        (current == OrderStatus.Delayed && next is OrderStatus.Shipped or OrderStatus.OutForDelivery) || (int)next >= (int)current;
    private void SetStatus(Order order, OrderStatus status, Guid? changedBy, string? note)
    {
        order.Status = status; db.OrderStatusHistories.Add(new OrderStatusHistory { TenantId = order.TenantId,
            OrderId = order.Id, Status = status, ChangedBy = changedBy, Note = note });
    }
    private void MarkShipmentsDelivered(Guid orderId)
    {
        foreach (var shipment in db.Shipments.Where(s => s.OrderId == orderId && s.DeliveredAt == null)) { shipment.Status = OrderStatus.Delivered.ToString(); shipment.DeliveredAt = DateTime.UtcNow; }
    }
    private async Task<string> SaveFileAsync(Order order, string bucket, IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 10 * 1024 * 1024 || !AllowedFileTypes.Contains(file.ContentType)) throw ApiException.BadRequest("الملف يجب أن يكون صورة أو PDF وبحجم أقصى 10MB");
        var ext = file.ContentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".pdf" };
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "orders", order.TenantId.ToString("N"), order.Id.ToString("N"), bucket);
        Directory.CreateDirectory(folder); var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}");
        await using var stream = File.Create(absolute); await file.CopyToAsync(stream, ct);
        return Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/');
    }
    private string SafePath(string relative)
    {
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data")); var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw ApiException.NotFound("الملف غير موجود"); return path;
    }
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var clean = value.Trim(); return clean.Length <= max ? clean : clean[..max]; }
    private static string StatusLabel(OrderStatus status) => status switch { OrderStatus.Confirmed => "تم تأكيد الطلب", OrderStatus.Processing => "قيد المراجعة والتجهيز",
        OrderStatus.Picking => "جاري جمع المنتجات", OrderStatus.Packing => "جاري التغليف", OrderStatus.Shipped => "تم الشحن",
        OrderStatus.OutForDelivery => "خرج للتوصيل", OrderStatus.PartiallyDelivered => "تم تسليم جزء من الطلب", OrderStatus.Delivered => "تم التسليم",
        OrderStatus.Completed => "اكتمل الطلب", OrderStatus.Delayed => "يوجد تأخير في الشحنة", _ => status.ToString() };
}
