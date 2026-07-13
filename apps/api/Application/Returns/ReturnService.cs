using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Returns;

public sealed class ReturnService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment environment)
{
    private static readonly HashSet<string> AllowedImages = ["image/jpeg", "image/png"];

    public async Task<List<EligibleReturnOrderDto>> EligibleOrdersAsync(Guid userId, CancellationToken ct = default)
    {
        var orders = await db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.History)
            .Where(o => o.UserId == userId && (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed))
            .OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        var orderIds = orders.Select(o => o.Id).ToList();
        var used = await db.ReturnItems.AsNoTracking().Where(i => orderIds.Contains(i.ReturnRequest.OrderId) &&
                i.ReturnRequest.Status != ReturnStatus.Rejected && i.ReturnRequest.Status != ReturnStatus.Cancelled)
            .GroupBy(i => i.OrderItemId).Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) }).ToDictionaryAsync(x => x.Id, x => x.Qty, ct);
        var result = new List<EligibleReturnOrderDto>();
        foreach (var order in orders)
        {
            var delivered = order.History.Where(h => h.Status == OrderStatus.Delivered).OrderByDescending(h => h.CreatedAt).Select(h => (DateTime?)h.CreatedAt).FirstOrDefault() ?? order.UpdatedAt ?? order.CreatedAt;
            var until = delivered.AddDays(30); if (until < DateTime.UtcNow) continue;
            var items = order.Items.Select(i => new EligibleReturnItemDto(i.Id, i.Sku, i.NameAr, i.Quantity,
                used.GetValueOrDefault(i.Id), Math.Max(0, i.Quantity - used.GetValueOrDefault(i.Id)), i.UnitPrice)).Where(i => i.EligibleQuantity > 0).ToList();
            if (items.Count > 0) result.Add(new(order.Id, order.Number, delivered, until, order.DeliveryAddress, items));
        }
        return result;
    }

    public async Task<List<ReturnListDto>> ListAsync(Guid userId, string? status, CancellationToken ct = default)
    {
        var query = db.ReturnRequests.AsNoTracking().Include(r => r.Order).Include(r => r.Items).Where(r => r.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReturnStatus>(status, true, out var parsed)) query = query.Where(r => r.Status == parsed);
        return await query.OrderByDescending(r => r.CreatedAt).Select(r => new ReturnListDto(r.Id, r.Number, r.Order.Number,
            r.Status.ToString(), r.Resolution.ToString(), r.RequestedTotal, r.ApprovedTotal, r.Items.Count, r.CreatedAt, r.PickupAt)).ToListAsync(ct);
    }

    public async Task<ReturnDetailDto> CreateAsync(Guid userId, CreateReturnDto dto, CancellationToken ct = default)
    {
        if (dto.Items.Count == 0 || dto.Items.Select(i => i.OrderItemId).Distinct().Count() != dto.Items.Count) throw ApiException.BadRequest("اختر صنفًا واحدًا على الأقل بدون تكرار");
        if (!Enum.TryParse<ReturnResolution>(dto.Resolution, true, out var resolution)) throw ApiException.BadRequest("نوع التسوية غير صالح");
        RefundMethod? refundMethod = null;
        if (resolution == ReturnResolution.Refund)
        {
            if (!Enum.TryParse<RefundMethod>(dto.RefundMethod, true, out var method)) throw ApiException.BadRequest("اختر طريقة استرداد المبلغ");
            refundMethod = method;
        }
        var order = await db.Orders.Include(o => o.Items).Include(o => o.History).FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId, ct)
            ?? throw ApiException.NotFound("الطلب غير موجود");
        if (order.Status is not (OrderStatus.Delivered or OrderStatus.Completed)) throw ApiException.Conflict("يمكن إرجاع الطلبات المستلمة فقط");
        var delivered = order.History.Where(h => h.Status == OrderStatus.Delivered).OrderByDescending(h => h.CreatedAt).Select(h => (DateTime?)h.CreatedAt).FirstOrDefault() ?? order.UpdatedAt ?? order.CreatedAt;
        if (delivered.AddDays(30) < DateTime.UtcNow) throw ApiException.Conflict("انتهت فترة الإرجاع لهذا الطلب");
        var itemIds = dto.Items.Select(i => i.OrderItemId).ToList();
        var previous = await db.ReturnItems.Where(i => itemIds.Contains(i.OrderItemId) && i.ReturnRequest.Status != ReturnStatus.Rejected && i.ReturnRequest.Status != ReturnStatus.Cancelled)
            .GroupBy(i => i.OrderItemId).Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) }).ToDictionaryAsync(x => x.Id, x => x.Qty, ct);
        var request = new ReturnRequest { TenantId = TenantId(), UserId = userId, OrderId = order.Id,
            Number = $"RET-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}", Resolution = resolution,
            RefundMethod = refundMethod, PickupAddress = Clean(dto.PickupAddress, 500) ?? order.DeliveryAddress };
        foreach (var input in dto.Items)
        {
            var source = order.Items.FirstOrDefault(i => i.Id == input.OrderItemId) ?? throw ApiException.BadRequest("صنف الإرجاع لا ينتمي إلى الطلب");
            if (!Enum.TryParse<ReturnReason>(input.Reason, true, out var reason) || input.Quantity <= 0 || input.Quantity > source.Quantity - previous.GetValueOrDefault(source.Id))
                throw ApiException.BadRequest($"الكمية أو سبب الإرجاع غير صالح للصنف {source.NameAr}");
            if (reason == ReturnReason.Other && string.IsNullOrWhiteSpace(input.Description)) throw ApiException.BadRequest("اكتب وصف سبب الإرجاع");
            request.Items.Add(new ReturnItem { TenantId = TenantId(), OrderItemId = source.Id, Quantity = input.Quantity,
                Reason = reason, Description = Clean(input.Description, 1000), UnitRefund = source.UnitPrice,
                LineRefund = decimal.Round(source.UnitPrice * input.Quantity, 2) });
        }
        request.RequestedTotal = request.Items.Sum(i => i.LineRefund);
        request.History.Add(new ReturnStatusHistory { TenantId = TenantId(), Status = ReturnStatus.Draft, ChangedBy = userId, Note = "تم إنشاء مسودة طلب الإرجاع" });
        db.ReturnRequests.Add(request); await db.SaveChangesAsync(ct); return await DetailAsync(userId, request.Id, ct);
    }

    public async Task<ReturnAttachmentDto> UploadAsync(Guid userId, Guid id, Guid? itemId, IFormFile file, CancellationToken ct = default)
    {
        var request = await EditableAsync(userId, id, ct);
        if (itemId is not null && !request.Items.Any(i => i.Id == itemId)) throw ApiException.BadRequest("صنف المرتجع غير صالح");
        if (file.Length is <= 0 or > 10 * 1024 * 1024 || !AllowedImages.Contains(file.ContentType)) throw ApiException.BadRequest("الصورة يجب أن تكون JPG أو PNG وبحجم أقصى 10MB");
        var ext = file.ContentType == "image/png" ? ".png" : ".jpg"; var folder = Path.Combine(environment.ContentRootPath, "App_Data", "returns", TenantId().ToString("N"), request.Id.ToString("N"));
        Directory.CreateDirectory(folder); var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}"); await using (var stream = File.Create(absolute)) await file.CopyToAsync(stream, ct);
        var attachment = new ReturnAttachment { TenantId = TenantId(), ReturnRequestId = request.Id, ReturnItemId = itemId,
            OriginalName = Path.GetFileName(file.FileName), ContentType = file.ContentType, SizeBytes = file.Length,
            StoredPath = Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/') };
        db.ReturnAttachments.Add(attachment); await db.SaveChangesAsync(ct); return Map(attachment);
    }

    public async Task<ReturnDetailDto> SubmitAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var request = await EditableAsync(userId, id, ct);
        var needsPhoto = request.Items.Any(i => i.Reason is ReturnReason.Damaged or ReturnReason.QualityIssue);
        if (needsPhoto && !await db.ReturnAttachments.AnyAsync(a => a.ReturnRequestId == request.Id, ct)) throw ApiException.Conflict("أرفق صورة واضحة لحالة المنتج التالف");
        SetStatus(request, ReturnStatus.Submitted, userId, "تم إرسال طلب الإرجاع للمراجعة"); request.SubmittedAt = DateTime.UtcNow;
        Notify(request, "تم إرسال طلب الإرجاع", $"بدأت مراجعة {request.Number}"); await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<ReturnDetailDto> CancelAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var request = await OwnedAsync(userId, id, ct);
        if (request.Status is not (ReturnStatus.Draft or ReturnStatus.Submitted or ReturnStatus.UnderReview)) throw ApiException.Conflict("لا يمكن إلغاء المرتجع بعد اعتماده");
        SetStatus(request, ReturnStatus.Cancelled, userId, "ألغى العميل طلب الإرجاع"); request.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<ReturnDetailDto> DecisionAsync(Guid staffId, Guid id, string decision, ReturnDecisionDto dto, CancellationToken ct = default)
    {
        var request = await StaffOwnedAsync(id, ct);
        if (request.Status is not (ReturnStatus.Submitted or ReturnStatus.UnderReview)) throw ApiException.Conflict("طلب الإرجاع ليس في مرحلة المراجعة");
        if (decision.Equals("review", StringComparison.OrdinalIgnoreCase)) SetStatus(request, ReturnStatus.UnderReview, staffId, Clean(dto.Reason, 500));
        else if (decision.Equals("approve", StringComparison.OrdinalIgnoreCase)) SetStatus(request, ReturnStatus.Approved, staffId, Clean(dto.Reason, 500) ?? "تمت الموافقة على الإرجاع");
        else if (decision.Equals("reject", StringComparison.OrdinalIgnoreCase)) { if (string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("سبب الرفض مطلوب"); request.RejectionReason = Clean(dto.Reason, 1000); SetStatus(request, ReturnStatus.Rejected, staffId, request.RejectionReason); request.CompletedAt = DateTime.UtcNow; }
        else throw ApiException.BadRequest("قرار المراجعة غير صالح");
        Notify(request, "تحديث طلب الإرجاع", $"{request.Number}: {StatusLabel(request.Status)}"); await db.SaveChangesAsync(ct); return await DetailAsync(request.UserId, request.Id, ct);
    }

    public async Task<ReturnDetailDto> SchedulePickupAsync(Guid staffId, Guid id, ReturnPickupDto dto, CancellationToken ct = default)
    {
        var request = await StaffOwnedAsync(id, ct); if (request.Status != ReturnStatus.Approved || dto.PickupAt <= DateTime.UtcNow) throw ApiException.Conflict("لا يمكن جدولة الاستلام بهذه البيانات");
        request.PickupAt = dto.PickupAt; request.PickupWindow = Clean(dto.PickupWindow, 100); request.PickupDriverName = Clean(dto.DriverName, 150); request.PickupDriverPhone = Clean(dto.DriverPhone, 30);
        SetStatus(request, ReturnStatus.PickupScheduled, staffId, $"موعد الاستلام {dto.PickupAt:yyyy-MM-dd HH:mm}"); Notify(request, "تم تحديد موعد استلام المرتجع", $"سيتم الاستلام في {dto.PickupAt:yyyy-MM-dd HH:mm}");
        await db.SaveChangesAsync(ct); return await DetailAsync(request.UserId, id, ct);
    }

    public async Task<ReturnDetailDto> TrackPickupAsync(Guid staffId, Guid id, ReturnTrackingDto dto, CancellationToken ct = default)
    {
        var request = await StaffOwnedAsync(id, ct); if (request.Status is not (ReturnStatus.PickupScheduled or ReturnStatus.InTransit)) throw ApiException.Conflict("مندوب الاستلام غير نشط لهذا الطلب");
        request.PickupLatitude = dto.Latitude; request.PickupLongitude = dto.Longitude; SetStatus(request, ReturnStatus.InTransit, staffId, Clean(dto.Note, 500) ?? Clean(dto.Location, 200) ?? "المندوب في الطريق");
        await db.SaveChangesAsync(ct); return await DetailAsync(request.UserId, id, ct);
    }

    public async Task<ReturnDetailDto> InspectAsync(Guid staffId, Guid id, ReturnInspectionDto dto, CancellationToken ct = default)
    {
        var request = await StaffOwnedAsync(id, ct); if (request.Status is not (ReturnStatus.Received or ReturnStatus.Inspecting)) throw ApiException.Conflict("يجب استلام المرتجع قبل الفحص");
        request.InspectionNotes = Clean(dto.Notes, 1500); foreach (var item in request.Items) item.InspectionPassed = dto.Passed;
        if (!dto.Passed) { request.RejectionReason = request.InspectionNotes ?? "لم يجتز المنتج الفحص"; SetStatus(request, ReturnStatus.Rejected, staffId, request.RejectionReason); request.CompletedAt = DateTime.UtcNow; }
        else { request.ApprovedTotal = dto.ApprovedTotal ?? request.RequestedTotal; if (request.ApprovedTotal is <= 0 or > 100000000) throw ApiException.BadRequest("قيمة الاسترداد المعتمدة غير صالحة"); SetStatus(request, request.Resolution == ReturnResolution.Refund ? ReturnStatus.RefundApproved : ReturnStatus.ReplacementPreparing, staffId, request.InspectionNotes ?? "اجتاز المنتج الفحص"); }
        Notify(request, "ظهرت نتيجة فحص المرتجع", $"{request.Number}: {StatusLabel(request.Status)}"); await db.SaveChangesAsync(ct); return await DetailAsync(request.UserId, id, ct);
    }

    public async Task<ReturnDetailDto> ProgressAsync(Guid staffId, Guid id, ReturnProgressDto dto, CancellationToken ct = default)
    {
        var request = await StaffOwnedAsync(id, ct); var action = dto.Action.Trim().ToLowerInvariant();
        switch (action)
        {
            case "received" when request.Status == ReturnStatus.InTransit: request.ReceivedAt = DateTime.UtcNow; SetStatus(request, ReturnStatus.Received, staffId, Clean(dto.Note, 500) ?? "تم استلام المرتجع"); break;
            case "inspecting" when request.Status == ReturnStatus.Received: SetStatus(request, ReturnStatus.Inspecting, staffId, Clean(dto.Note, 500)); break;
            case "refund-complete" when request.Status == ReturnStatus.RefundApproved:
                var transaction = new RefundTransaction { TenantId = request.TenantId, ReturnRequestId = request.Id, Method = request.RefundMethod ?? RefundMethod.CreditBalance,
                    Amount = request.ApprovedTotal ?? request.RequestedTotal, Status = RefundTransactionStatus.Completed,
                    ProviderReference = Clean(dto.ProviderReference, 100) ?? $"RFND-{Guid.NewGuid():N}", CompletedAt = DateTime.UtcNow };
                db.RefundTransactions.Add(transaction); SetStatus(request, ReturnStatus.RefundCompleted, staffId, Clean(dto.Note, 500) ?? "تم رد المبلغ"); break;
            case "replacement-shipped" when request.Status == ReturnStatus.ReplacementPreparing: SetStatus(request, ReturnStatus.ReplacementShipped, staffId, Clean(dto.Note, 500)); break;
            case "replacement-delivered" when request.Status == ReturnStatus.ReplacementShipped: SetStatus(request, ReturnStatus.ReplacementDelivered, staffId, Clean(dto.Note, 500)); break;
            case "complete" when request.Status is ReturnStatus.RefundCompleted or ReturnStatus.ReplacementDelivered: SetStatus(request, ReturnStatus.Completed, staffId, Clean(dto.Note, 500) ?? "اكتمل طلب الإرجاع"); request.CompletedAt = DateTime.UtcNow; break;
            default: throw ApiException.Conflict("إجراء المرتجع غير متاح في حالته الحالية");
        }
        Notify(request, "تحديث طلب الإرجاع", $"{request.Number}: {StatusLabel(request.Status)}"); await db.SaveChangesAsync(ct); return await DetailAsync(request.UserId, id, ct);
    }

    public async Task<ReturnDetailDto> DetailAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var request = await db.ReturnRequests.AsNoTracking().Include(r => r.Order).Include(r => r.Items).ThenInclude(i => i.OrderItem)
            .Include(r => r.Attachments).Include(r => r.History).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct) ?? throw ApiException.NotFound("طلب الإرجاع غير موجود");
        return new(request.Id, request.Number, request.OrderId, request.Order.Number, request.Status.ToString(), request.Resolution.ToString(), request.RefundMethod?.ToString(),
            request.RequestedTotal, request.ApprovedTotal, request.PickupAddress, request.PickupAt, request.PickupWindow, request.PickupDriverName, request.PickupDriverPhone,
            request.PickupLatitude, request.PickupLongitude, request.RejectionReason, request.InspectionNotes, request.SubmittedAt, request.ReceivedAt, request.CompletedAt,
            request.Items.Select(i => new ReturnItemDto(i.Id, i.OrderItemId, i.OrderItem.Sku, i.OrderItem.NameAr, i.Quantity, i.Reason.ToString(), i.Description,
                i.UnitRefund, i.LineRefund, i.IsEligible, i.EligibilityNote, i.InspectionPassed)).ToList(), request.Attachments.Select(Map).ToList(),
            request.History.OrderBy(h => h.CreatedAt).Select(h => new ReturnHistoryDto(h.Status.ToString(), h.Note, h.CreatedAt)).ToList(),
            request.Status == ReturnStatus.Draft, request.Status is ReturnStatus.Draft or ReturnStatus.Submitted or ReturnStatus.UnderReview,
            request.Status is ReturnStatus.PickupScheduled or ReturnStatus.InTransit);
    }

    public async Task<(string Path, string Type, string Name)> FileAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var file = await db.ReturnAttachments.AsNoTracking().Include(a => a.ReturnRequest).FirstOrDefaultAsync(a => a.Id == id && a.ReturnRequest.UserId == userId, ct) ?? throw ApiException.NotFound("صورة المرتجع غير موجودة");
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data")); var path = Path.GetFullPath(Path.Combine(environment.ContentRootPath, file.StoredPath));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(path)) throw ApiException.NotFound("الملف غير موجود"); return (path, file.ContentType, file.OriginalName);
    }

    private async Task<ReturnRequest> EditableAsync(Guid userId, Guid id, CancellationToken ct) { var r = await OwnedAsync(userId, id, ct); if (r.Status != ReturnStatus.Draft) throw ApiException.Conflict("لا يمكن تعديل المرتجع بعد الإرسال"); return r; }
    private async Task<ReturnRequest> OwnedAsync(Guid userId, Guid id, CancellationToken ct) => await db.ReturnRequests.Include(r => r.Items).Include(r => r.History).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct) ?? throw ApiException.NotFound("طلب الإرجاع غير موجود");
    private async Task<ReturnRequest> StaffOwnedAsync(Guid id, CancellationToken ct) => await db.ReturnRequests.IgnoreQueryFilters().Include(r => r.Items).Include(r => r.History).FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct) ?? throw ApiException.NotFound("طلب الإرجاع غير موجود");
    private void SetStatus(ReturnRequest r, ReturnStatus status, Guid? actor, string? note) { r.Status = status; db.ReturnStatusHistories.Add(new ReturnStatusHistory { TenantId = r.TenantId, ReturnRequestId = r.Id, Status = status, ChangedBy = actor, Note = note }); }
    private void Notify(ReturnRequest r, string title, string body) => db.Notifications.Add(new AppNotification { TenantId = r.TenantId, UserId = r.UserId, Type = "return.status", Title = title, Body = body, EntityType = nameof(ReturnRequest), EntityId = r.Id });
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("تعذر تحديد الشركة");
    private static ReturnAttachmentDto Map(ReturnAttachment a) => new(a.Id, a.ReturnItemId, a.OriginalName, a.ContentType, a.SizeBytes, a.CreatedAt);
    private static string? Clean(string? v, int max) { if (string.IsNullOrWhiteSpace(v)) return null; var value = v.Trim(); return value.Length <= max ? value : value[..max]; }
    private static string StatusLabel(ReturnStatus status) => status switch { ReturnStatus.Submitted => "قيد المراجعة", ReturnStatus.Approved => "تمت الموافقة", ReturnStatus.Rejected => "مرفوض", ReturnStatus.PickupScheduled => "تم تحديد الاستلام", ReturnStatus.InTransit => "المندوب في الطريق", ReturnStatus.Received => "تم الاستلام", ReturnStatus.RefundApproved => "تم اعتماد الاسترداد", ReturnStatus.RefundCompleted => "تم رد المبلغ", ReturnStatus.ReplacementPreparing => "جاري تجهيز البديل", ReturnStatus.ReplacementShipped => "تم شحن البديل", ReturnStatus.ReplacementDelivered => "تم تسليم البديل", ReturnStatus.Completed => "مكتمل", _ => status.ToString() };
}
