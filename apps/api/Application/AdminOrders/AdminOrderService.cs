using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Application.Orders;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminOrders;

public sealed class AdminOrderService(AppDbContext db, OrderService orderService, FinanceService finance)
{
    private static readonly OrderStatus[] Finished = [OrderStatus.Delivered, OrderStatus.Completed, OrderStatus.Cancelled];

    public async Task<AdminOrderPageDto> ListAsync(string? search, string? statuses, Guid? tenantId,
        Guid? assignedStaffId, DateTime? from, DateTime? to, bool overdue = false, bool archived = false,
        int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = from order in db.Orders.AsNoTracking()
                    join company in db.Companies.AsNoTracking() on order.TenantId equals company.TenantId
                    join customer in db.Users.AsNoTracking() on order.UserId equals customer.Id
                    where archived ? order.ArchivedAt != null : order.ArchivedAt == null
                    select new { order, company, customer };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.order.Number.Contains(term) || x.company.LegalName.Contains(term) ||
                (x.order.PurchaseOrderNumber != null && x.order.PurchaseOrderNumber.Contains(term)));
        }
        var parsedStatuses = (statuses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Enum.TryParse<OrderStatus>(x, true, out var value) ? value : (OrderStatus?)null)
            .Where(x => x is not null).Select(x => x!.Value).ToList();
        if (parsedStatuses.Count > 0) query = query.Where(x => parsedStatuses.Contains(x.order.Status));
        if (tenantId is not null) query = query.Where(x => x.order.TenantId == tenantId);
        if (assignedStaffId is not null) query = query.Where(x => x.order.AssignedStaffId == assignedStaffId);
        if (from is not null) query = query.Where(x => x.order.CreatedAt >= from.Value.Date);
        if (to is not null) query = query.Where(x => x.order.CreatedAt < to.Value.Date.AddDays(1));
        if (overdue) query = query.Where(x => x.order.RequiredDate < DateTime.UtcNow && !Finished.Contains(x.order.Status));

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.order.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.order.Id, x.order.Number, x.company.LegalName, x.customer.FullName, x.order.Status, x.order.Total,
                ItemCount = x.order.Items.Count, x.order.CreatedAt, x.order.RequiredDate, x.order.PurchaseOrderNumber,
                x.order.AssignedStaffId, x.order.ArchivedAt,
            }).ToListAsync(ct);
        var staffIds = rows.Where(x => x.AssignedStaffId != null).Select(x => x.AssignedStaffId!.Value).Distinct().ToList();
        var staff = await db.Users.AsNoTracking().Where(x => staffIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var summaryQuery = db.Orders.AsNoTracking();
        var now = DateTime.UtcNow;
        var summary = new AdminOrderSummaryDto(
            await summaryQuery.CountAsync(x => x.ArchivedAt == null, ct),
            await summaryQuery.CountAsync(x => x.ArchivedAt == null && x.CreatedAt >= now.Date, ct),
            await summaryQuery.CountAsync(x => x.ArchivedAt == null && (x.Status == OrderStatus.Processing || x.Status == OrderStatus.Picking || x.Status == OrderStatus.Packing), ct),
            await summaryQuery.CountAsync(x => x.ArchivedAt == null && x.Status == OrderStatus.OutForDelivery, ct),
            await summaryQuery.CountAsync(x => x.ArchivedAt == null && x.RequiredDate < now && !Finished.Contains(x.Status), ct),
            await summaryQuery.CountAsync(x => x.ArchivedAt != null, ct));
        return new(rows.Select(x => new AdminOrderRowDto(x.Id, x.Number, x.LegalName, x.FullName,
            x.Status.ToString(), x.Total, x.ItemCount, x.CreatedAt, x.RequiredDate, x.PurchaseOrderNumber,
            x.AssignedStaffId, x.AssignedStaffId is { } id ? staff.GetValueOrDefault(id) : null,
            x.RequiredDate < now && !Finished.Contains(x.Status), x.ArchivedAt is not null)).ToList(), total, page, pageSize, summary);
    }

    public async Task<AdminOrderDetailDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetOrderAsync(id, ct);
        var order = await orderService.DetailAsync(entity.UserId, entity.Id, ct);
        var company = await db.Companies.AsNoTracking().FirstAsync(x => x.TenantId == entity.TenantId, ct);
        var customer = await db.Users.AsNoTracking().FirstAsync(x => x.Id == entity.UserId, ct);
        var staffName = entity.AssignedStaffId is { } assigned
            ? await db.Users.AsNoTracking().Where(x => x.Id == assigned).Select(x => x.FullName).FirstOrDefaultAsync(ct) : null;
        var products = await db.Products.AsNoTracking().Where(x => entity.Items.Select(i => i.ProductId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var notesRaw = await db.OrderInternalNotes.AsNoTracking().Where(x => x.OrderId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var communicationsRaw = await db.OrderCommunications.AsNoTracking().Where(x => x.OrderId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var actorIds = notesRaw.Select(x => x.StaffUserId).Concat(communicationsRaw.Select(x => x.StaffUserId)).Distinct().ToList();
        var actors = await db.Users.AsNoTracking().Where(x => actorIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var refunds = await db.AdminOrderRefunds.AsNoTracking().Where(x => x.OrderId == id).OrderByDescending(x => x.ProcessedAt).ToListAsync(ct);
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.OrderId == id, ct);
        var shipments = await db.Shipments.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var shipmentIds = shipments.Select(x => x.Id).ToList();
        var mapped = await db.ShipmentItems.AsNoTracking().Where(x => shipmentIds.Contains(x.ShipmentId)).ToListAsync(ct);
        var itemNames = entity.Items.ToDictionary(x => x.Id, x => x.NameAr);
        return new(order,
            new(company.TenantId, company.LegalName, company.TaxCardNo, company.Industry, company.Governorate, company.Phone, company.Email, company.CreditLimit, company.CreditUsed),
            new(customer.Id, customer.FullName, customer.Phone, customer.Email, customer.JobTitle, customer.Department),
            staffName, entity.ArchivedAt,
            entity.Items.Select(item =>
            {
                var product = products.GetValueOrDefault(item.ProductId);
                return new AdminOrderProductDto(item.Id, item.ProductId, item.Sku, item.NameAr, item.Quantity,
                    item.UnitPrice, item.LineTotal, product?.StockQty ?? 0, product?.GetStockStatus().ToString() ?? "Unavailable");
            }).ToList(),
            notesRaw.Select(x => new AdminOrderNoteDto(x.Id, x.StaffUserId, actors.GetValueOrDefault(x.StaffUserId, "موظف"), x.Body, x.CreatedAt)).ToList(),
            communicationsRaw.Select(x => new AdminOrderCommunicationDto(x.Id, actors.GetValueOrDefault(x.StaffUserId, "موظف"), x.Channel, x.Direction, x.Subject, x.Body, x.CreatedAt)).ToList(),
            refunds.Select(x => new AdminOrderRefundDto(x.Id, x.Amount, x.Method, x.Reason, x.Reference, x.Status.ToString(), x.ProcessedAt)).ToList(),
            invoice is null ? null : new(invoice.Id, invoice.Number, invoice.Status.ToString(), invoice.Total, invoice.PaidAmount, invoice.IssuedAt, invoice.DueAt),
            shipments.Select(x => new AdminShipmentDto(x.Id, x.Number, x.Status, x.CarrierName, x.CreatedAt,
                mapped.Where(m => m.ShipmentId == x.Id).Select(m => new AdminShipmentItemDto(m.OrderItemId, itemNames.GetValueOrDefault(m.OrderItemId, "صنف"), m.Quantity)).ToList())).ToList());
    }

    public async Task<AdminOrderDetailDto> UpdateQuantitiesAsync(Guid staffId, Guid id, UpdateAdminOrderQuantitiesDto dto, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); EnsureEditable(order);
        if (dto.Items.Count == 0 || dto.Items.Any(x => x.Quantity is < 1 or > 100000) || dto.Items.Select(x => x.ItemId).Distinct().Count() != dto.Items.Count)
            throw ApiException.BadRequest("الكميات غير صالحة");
        foreach (var update in dto.Items)
        {
            var item = order.Items.FirstOrDefault(x => x.Id == update.ItemId) ?? throw ApiException.BadRequest("صنف غير موجود في الطلب");
            item.Quantity = update.Quantity; item.LineTotal = item.UnitPrice * item.Quantity;
        }
        Recalculate(order); AddHistory(order, staffId, $"تعديل الكميات: {Clean(dto.Reason, 300) ?? "تحديث إداري"}");
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminOrderDetailDto> SubstituteAsync(Guid staffId, Guid id, SubstituteOrderProductDto dto, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); EnsureEditable(order);
        var item = order.Items.FirstOrDefault(x => x.Id == dto.ItemId) ?? throw ApiException.BadRequest("صنف الطلب غير موجود");
        var replacement = await db.Products.FirstOrDefaultAsync(x => x.Id == dto.ProductId && x.Status == ProductStatus.Active, ct)
            ?? throw ApiException.NotFound("المنتج البديل غير متاح");
        if (replacement.StockQty < item.Quantity) throw ApiException.Conflict("مخزون المنتج البديل لا يغطي الكمية");
        var original = item.NameAr; item.ProductId = replacement.Id; item.VariantId = null; item.Sku = replacement.Sku;
        item.NameAr = replacement.NameAr; item.VariantName = null; item.UnitPrice = replacement.BasePrice; item.LineTotal = item.Quantity * item.UnitPrice;
        Recalculate(order); AddHistory(order, staffId, $"استبدال {original} بـ {replacement.NameAr}: {Clean(dto.Reason, 250)}");
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminOrderDetailDto> SplitAsync(Guid staffId, Guid id, SplitOrderShipmentsDto dto, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); EnsureEditable(order);
        if (dto.Shipments.Count < 2) throw ApiException.BadRequest("يجب إنشاء شحنتين على الأقل");
        if (await db.ShipmentItems.AnyAsync(x => db.Shipments.Where(s => s.OrderId == id).Select(s => s.Id).Contains(x.ShipmentId), ct))
            throw ApiException.Conflict("تم تقسيم الطلب بالفعل");
        var allocated = dto.Shipments.SelectMany(x => x.Items).GroupBy(x => x.OrderItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        if (order.Items.Any(x => !allocated.TryGetValue(x.Id, out var quantity) || quantity != x.Quantity) || dto.Shipments.SelectMany(x => x.Items).Any(x => x.Quantity <= 0))
            throw ApiException.BadRequest("يجب توزيع كل كميات الطلب بدقة على الشحنات");
        foreach (var input in dto.Shipments)
        {
            var shipment = new Shipment { TenantId = order.TenantId, OrderId = order.Id,
                Number = Clean(input.Number, 50) ?? $"SHP-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                CarrierName = Clean(input.Carrier, 100) ?? "Mohandseto Logistics", Status = "Created" };
            db.Shipments.Add(shipment);
            foreach (var item in input.Items) db.ShipmentItems.Add(new ShipmentItem { TenantId = order.TenantId, ShipmentId = shipment.Id, OrderItemId = item.OrderItemId, Quantity = item.Quantity });
        }
        order.AllowSplitDelivery = true; AddHistory(order, staffId, $"تقسيم الطلب إلى {dto.Shipments.Count} شحنات");
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminOrderDetailDto> AssignAsync(Guid staffId, Guid id, Guid assigneeId, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct);
        var assignee = await db.Users.FirstOrDefaultAsync(x => x.Id == assigneeId && x.IsPlatformStaff && x.IsActive, ct)
            ?? throw ApiException.BadRequest("الموظف المحدد غير صالح");
        order.AssignedStaffId = assignee.Id; AddHistory(order, staffId, $"تخصيص الطلب إلى {assignee.FullName}");
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminOrderNoteDto> AddNoteAsync(Guid staffId, Guid id, string body, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); var clean = Clean(body, 1500) ?? throw ApiException.BadRequest("الملاحظة مطلوبة");
        var note = new OrderInternalNote { TenantId = order.TenantId, OrderId = id, StaffUserId = staffId, Body = clean };
        db.OrderInternalNotes.Add(note); await db.SaveChangesAsync(ct);
        var name = await db.Users.Where(x => x.Id == staffId).Select(x => x.FullName).FirstOrDefaultAsync(ct) ?? "موظف";
        return new(note.Id, staffId, name, note.Body, note.CreatedAt);
    }

    public async Task<AdminOrderCommunicationDto> AddCommunicationAsync(Guid staffId, Guid id, AddOrderCommunicationDto dto, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); var subject = Clean(dto.Subject, 200) ?? throw ApiException.BadRequest("عنوان التواصل مطلوب");
        var allowedChannels = new[] { "Phone", "Email", "WhatsApp", "Chat", "Visit" };
        var allowedDirections = new[] { "Inbound", "Outbound" };
        if (!allowedChannels.Contains(dto.Channel) || !allowedDirections.Contains(dto.Direction)) throw ApiException.BadRequest("بيانات التواصل غير صالحة");
        var log = new OrderCommunication { TenantId = order.TenantId, OrderId = id, StaffUserId = staffId,
            Channel = dto.Channel, Direction = dto.Direction, Subject = subject, Body = Clean(dto.Body, 1500) };
        db.OrderCommunications.Add(log); await db.SaveChangesAsync(ct);
        var name = await db.Users.Where(x => x.Id == staffId).Select(x => x.FullName).FirstOrDefaultAsync(ct) ?? "موظف";
        return new(log.Id, name, log.Channel, log.Direction, log.Subject, log.Body, log.CreatedAt);
    }

    public async Task<AdminOrderInvoiceDto> IssueInvoiceAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await db.Invoices.FirstOrDefaultAsync(x => x.OrderId == id, ct);
        if (existing is not null) return new(existing.Id, existing.Number, existing.Status.ToString(), existing.Total, existing.PaidAmount, existing.IssuedAt, existing.DueAt);
        var order = await GetOrderAsync(id, ct); var invoice = finance.IssueForOrder(order); await db.SaveChangesAsync(ct);
        return new(invoice.Id, invoice.Number, invoice.Status.ToString(), invoice.Total, invoice.PaidAmount, invoice.IssuedAt, invoice.DueAt);
    }

    public async Task<AdminOrderDetailDto> CancelAsync(Guid staffId, Guid id, AdminCancelOrderDto dto, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct);
        if (order.Status is OrderStatus.Delivered or OrderStatus.Completed or OrderStatus.Cancelled) throw ApiException.Conflict("لا يمكن إلغاء الطلب في حالته الحالية");
        var reason = Clean(dto.Reason, 150) ?? throw ApiException.BadRequest("سبب الإلغاء مطلوب");
        db.OrderCancellations.Add(new OrderCancellation { TenantId = order.TenantId, OrderId = id, UserId = staffId, Reason = reason, Details = Clean(dto.Details, 1000), CancelledAt = DateTime.UtcNow });
        order.Status = OrderStatus.Cancelled; AddHistory(order, staffId, $"إلغاء إداري: {reason}"); await db.SaveChangesAsync(ct);
        return await DetailAsync(id, ct);
    }

    public async Task<AdminOrderRefundDto> RefundAsync(Guid staffId, Guid id, ProcessAdminRefundDto dto, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); if (dto.Amount <= 0) throw ApiException.BadRequest("المبلغ غير صالح");
        var refunded = await db.AdminOrderRefunds.Where(x => x.OrderId == id && x.Status == AdminOrderRefundStatus.Processed).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        if (refunded + dto.Amount > order.Total) throw ApiException.Conflict("إجمالي الاسترداد يتجاوز قيمة الطلب");
        if (string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("سبب الاسترداد مطلوب");
        var refund = new AdminOrderRefund { TenantId = order.TenantId, OrderId = id, ProcessedBy = staffId,
            Amount = dto.Amount, Method = Clean(dto.Method, 50) ?? "CreditBalance", Reason = Clean(dto.Reason, 500)!,
            Reference = $"RFD-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}" };
        db.AdminOrderRefunds.Add(refund); await db.SaveChangesAsync(ct);
        return new(refund.Id, refund.Amount, refund.Method, refund.Reason, refund.Reference, refund.Status.ToString(), refund.ProcessedAt);
    }

    public async Task ArchiveAsync(Guid staffId, Guid id, bool restore, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); order.ArchivedAt = restore ? null : DateTime.UtcNow; order.ArchivedBy = restore ? null : staffId;
        AddHistory(order, staffId, restore ? "استعادة الطلب من الأرشيف" : "أرشفة الطلب"); await db.SaveChangesAsync(ct);
    }

    public async Task<List<AdminRecurringDto>> RecurringAsync(CancellationToken ct = default)
    {
        return await (from schedule in db.RecurringOrderSchedules.AsNoTracking()
                      join order in db.Orders.AsNoTracking() on schedule.SourceOrderId equals order.Id
                      join company in db.Companies.AsNoTracking() on order.TenantId equals company.TenantId
                      orderby schedule.NextRunAt
                      select new AdminRecurringDto(schedule.Id, order.Number, company.LegalName, schedule.Frequency,
                          schedule.Interval, schedule.NextRunAt, schedule.EndsAt, schedule.IsActive, order.Total)).ToListAsync(ct);
    }

    public async Task<List<AdminStaffOptionDto>> StaffAsync(CancellationToken ct = default)
    {
        return await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff && x.IsActive)
            .OrderBy(x => x.FullName).Select(x => new AdminStaffOptionDto(x.Id, x.FullName, x.JobTitle,
                x.Department, db.Orders.Count(o => o.AssignedStaffId == x.Id && o.ArchivedAt == null && !Finished.Contains(o.Status))))
            .ToListAsync(ct);
    }

    public async Task UpdateRecurringAsync(Guid id, UpdateRecurringAdminDto dto, CancellationToken ct = default)
    {
        var schedule = await db.RecurringOrderSchedules.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الجدولة غير موجودة");
        if (dto.NextRunAt is not null && dto.NextRunAt <= DateTime.UtcNow) throw ApiException.BadRequest("موعد التنفيذ يجب أن يكون في المستقبل");
        schedule.IsActive = dto.IsActive; if (dto.NextRunAt is not null) schedule.NextRunAt = dto.NextRunAt.Value; await db.SaveChangesAsync(ct);
    }

    public async Task<List<PickingItemDto>> PickingAsync(Guid id, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); return order.Items.OrderBy(x => x.Sku).Select((x, index) =>
            new PickingItemDto(x.Id, x.Sku, x.NameAr, x.Quantity, $"A-{index / 5 + 1:D2}-{index % 5 + 1}", false)).ToList();
    }

    public async Task<List<PackingPackageDto>> PackingAsync(Guid id, CancellationToken ct = default)
    {
        var order = await GetOrderAsync(id, ct); var packages = new List<PackingPackageDto>(); var number = 1;
        foreach (var chunk in order.Items.Chunk(4))
            packages.Add(new(number++, chunk.Select(x => new AdminShipmentItemDto(x.Id, x.NameAr, x.Quantity)).ToList(), Math.Round(chunk.Sum(x => x.Quantity) * 0.45m, 2)));
        return packages;
    }

    private async Task<Order> GetOrderAsync(Guid id, CancellationToken ct) => await db.Orders.Include(x => x.Items).Include(x => x.History)
        .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الطلب غير موجود");
    private static void EnsureEditable(Order order) { if (order.Status >= OrderStatus.Shipped || order.Status == OrderStatus.Cancelled) throw ApiException.Conflict("لا يمكن تعديل أصناف الطلب بعد الشحن"); }
    private static void Recalculate(Order order) { order.Subtotal = order.Items.Sum(x => x.LineTotal); order.Total = Math.Max(0, order.Subtotal - order.Savings - order.CouponDiscount + order.Shipping); }
    private void AddHistory(Order order, Guid staffId, string note) => db.OrderStatusHistories.Add(new OrderStatusHistory { TenantId = order.TenantId, OrderId = order.Id, Status = order.Status, ChangedBy = staffId, Note = note });
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var clean = value.Trim(); return clean.Length <= max ? clean : clean[..max]; }
}
