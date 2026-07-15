using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminShipping;

public sealed class AdminShippingService(AppDbContext db, IWebHostEnvironment environment)
{
    private static readonly string[] ActiveStatuses = ["Ready", "Assigned", "OutForDelivery", "Rescheduled"];
    private static readonly string[] AllowedProofs = ["Photo", "Signature", "Document"];

    public async Task<ShippingDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        await EnsureDefaultZonesAsync(ct);
        var rows = await ShipmentRows().OrderByDescending(x => x.Shipment.CreatedAt).Take(250).ToListAsync(ct);
        var shipments = rows.Select(MapRow).ToList();
        var today = DateTime.UtcNow.Date;
        var completed = rows.Where(x => x.Shipment.Status == "Delivered").ToList();
        var kpis = new ShippingKpisDto(
            rows.Count(x => x.Shipment.Status == "Ready"), rows.Count(x => x.Shipment.Status == "Assigned"),
            rows.Count(x => x.Shipment.Status == "OutForDelivery"),
            rows.Count(x => x.Shipment.DeliveredAt >= today), rows.Count(x => x.Shipment.FailedAt >= today),
            Percent(completed.Count(x => x.Shipment.DeliveredAt <= x.Order.RequiredDate.Date.AddDays(1)), completed.Count),
            Percent(completed.Count(x => x.Shipment.DeliveryAttempt <= 1), completed.Count));
        var ordersWithShipments = rows.Select(x => x.Order.Id).Distinct().ToList();
        var ready = await (from o in db.Orders.AsNoTracking()
                           join c in db.Companies.AsNoTracking() on o.TenantId equals c.TenantId
                           where !ordersWithShipments.Contains(o.Id) &&
                              (o.Status == OrderStatus.Packing || o.Status == OrderStatus.Picking || o.Status == OrderStatus.Processing)
                           orderby o.RequiredDate
                           select new ReadyOrderDto(o.Id, o.Number, c.LegalName, o.DeliveryAddress, o.ReceiverName,
                               o.ReceiverPhone, o.RequiredDate, o.Items.Count, o.AllowSplitDelivery)).Take(100).ToListAsync(ct);
        var couriers = await CouriersAsync(rows, ct);
        var routes = await RoutesAsync(ct);
        var zoneEntities = await db.DeliveryZones.AsNoTracking().OrderBy(x => x.Governorate).ThenBy(x => x.NameAr).ToListAsync(ct);
        var zones = zoneEntities.Select(z => new ShippingZoneDto(z.Id, z.NameAr, z.Governorate, z.CitiesCsv,
            z.BaseFee, z.FeePerKg, z.FeePerKm, z.EstimatedDays, z.IsActive,
            rows.Count(x => x.Shipment.DeliveryZone == z.NameAr),
            rows.Where(x => x.Shipment.DeliveryZone == z.NameAr).Sum(x => x.Shipment.DeliveryCost))).ToList();
        return new(kpis, shipments, ready, couriers, routes, zones);
    }

    public async Task<ShipmentDetailDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        var row = await ShipmentRows().FirstOrDefaultAsync(x => x.Shipment.Id == id, ct)
            ?? throw ApiException.NotFound("الشحنة غير موجودة");
        var mapped = await db.ShipmentItems.AsNoTracking().Where(x => x.ShipmentId == id).ToListAsync(ct);
        var names = await db.OrderItems.AsNoTracking().Where(x => x.OrderId == row.Order.Id).ToDictionaryAsync(x => x.Id, x => x.NameAr, ct);
        var events = await db.ShipmentEvents.AsNoTracking().Where(x => x.ShipmentId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var proofs = await db.DeliveryProofs.AsNoTracking().Where(x => x.ShipmentId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return new(MapRow(row), mapped.Select(x => new ShippingItemDto(x.OrderItemId, names.GetValueOrDefault(x.OrderItemId, "صنف"), x.Quantity)).ToList(),
            events.Select(x => new ShippingEventDto(x.Id, x.Status, x.DescriptionAr, x.Location, x.Latitude, x.Longitude, x.CreatedAt)).ToList(),
            proofs.Select(x => new ShippingProofDto(x.Id, x.Type.ToString(), x.RecipientName, x.Note, x.Latitude, x.Longitude, x.StoredPath != null, x.CreatedAt)).ToList());
    }

    public async Task<ShipmentDetailDto> CreateAsync(Guid actor, CreateShipmentDto dto, CancellationToken ct = default)
    {
        if (dto.WeightKg <= 0 || dto.WeightKg > 100000) throw ApiException.BadRequest("وزن الشحنة غير صالح");
        var order = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct)
            ?? throw ApiException.NotFound("الطلب غير موجود");
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered or OrderStatus.Completed)
            throw ApiException.Conflict("حالة الطلب لا تسمح بإنشاء شحنة");
        var allocated = await (from si in db.ShipmentItems
                               join s in db.Shipments on si.ShipmentId equals s.Id
                               where s.OrderId == order.Id && s.Status != "Cancelled"
                               group si by si.OrderItemId into g select new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty, ct);
        var inputs = dto.Items?.Count > 0 ? dto.Items : order.Items.Select(x => new ShippingItemInputDto(x.Id, x.Quantity - allocated.GetValueOrDefault(x.Id))).Where(x => x.Quantity > 0).ToList();
        ValidateAllocation(order.Items, inputs!, allocated);
        var zone = !string.IsNullOrWhiteSpace(dto.Zone) ? await db.DeliveryZones.FirstOrDefaultAsync(x => x.NameAr == dto.Zone && x.IsActive, ct) : null;
        var cost = dto.DeliveryCost ?? (zone is null ? order.Shipping : zone.BaseFee + zone.FeePerKg * dto.WeightKg);
        var shipment = new Shipment { TenantId = order.TenantId, OrderId = order.Id, Number = Number(),
            CarrierName = Clean(dto.Carrier, 100) ?? "Mohandseto Logistics", Status = "Ready", DeliveryZone = zone?.NameAr ?? Clean(dto.Zone, 100),
            WeightKg = dto.WeightKg, DeliveryCost = Math.Max(0, cost), ScheduledAt = dto.ScheduledAt,
            DestinationLatitude = dto.DestinationLatitude, DestinationLongitude = dto.DestinationLongitude, CreatedBy = actor };
        db.Shipments.Add(shipment);
        foreach (var i in inputs!) db.ShipmentItems.Add(new ShipmentItem { TenantId = order.TenantId, ShipmentId = shipment.Id, OrderItemId = i.OrderItemId, Quantity = i.Quantity, CreatedBy = actor });
        Event(shipment, "Ready", "الشحنة جاهزة للتخصيص والتوصيل");
        if (order.Status < OrderStatus.Packing) order.Status = OrderStatus.Packing;
        Audit(actor, order.TenantId, "shipping.shipment.create", shipment.Id, order.Number);
        await db.SaveChangesAsync(ct); return await DetailAsync(shipment.Id, ct);
    }

    public async Task<IReadOnlyList<ShipmentDetailDto>> SplitAsync(Guid actor, Guid id, SplitShippingDto dto, CancellationToken ct = default)
    {
        var source = await db.Shipments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("الشحنة غير موجودة");
        if (source.Status is not ("Ready" or "Created")) throw ApiException.Conflict("يمكن تقسيم الشحنة قبل تخصيصها فقط");
        if (dto.Shipments.Count < 2) throw ApiException.BadRequest("يلزم إنشاء شحنتين على الأقل");
        var sourceItems = await db.ShipmentItems.Where(x => x.ShipmentId == id).ToListAsync(ct);
        var totals = dto.Shipments.SelectMany(x => x.Items).GroupBy(x => x.OrderItemId).ToDictionary(x => x.Key, x => x.Sum(i => i.Quantity));
        if (dto.Shipments.Any(x => x.WeightKg <= 0 || x.Items.Count == 0) || dto.Shipments.SelectMany(x => x.Items).Any(x => x.Quantity <= 0) ||
            sourceItems.Any(x => totals.GetValueOrDefault(x.OrderItemId) != x.Quantity) || totals.Keys.Any(x => sourceItems.All(s => s.OrderItemId != x)))
            throw ApiException.BadRequest("يجب توزيع كل كميات الشحنة بدقة");
        source.Status = "Cancelled"; source.UpdatedBy = actor; foreach (var item in sourceItems) item.IsDeleted = true;
        var result = new List<Guid>(); var n = 1;
        foreach (var part in dto.Shipments)
        {
            var shipment = new Shipment { TenantId = source.TenantId, OrderId = source.OrderId,
                Number = Clean(part.Number, 50) ?? $"{source.Number}-{n++}", CarrierName = source.CarrierName, Status = "Ready",
                DeliveryZone = source.DeliveryZone, WeightKg = part.WeightKg, DeliveryCost = dto.Shipments.Count == 0 ? 0 : source.DeliveryCost / dto.Shipments.Count,
                ScheduledAt = source.ScheduledAt, DestinationLatitude = source.DestinationLatitude, DestinationLongitude = source.DestinationLongitude, CreatedBy = actor };
            db.Shipments.Add(shipment); result.Add(shipment.Id);
            foreach (var i in part.Items) db.ShipmentItems.Add(new ShipmentItem { TenantId = source.TenantId, ShipmentId = shipment.Id, OrderItemId = i.OrderItemId, Quantity = i.Quantity, CreatedBy = actor });
            Event(shipment, "Ready", $"تم إنشاؤها من تقسيم الشحنة {source.Number}");
        }
        source.Order.AllowSplitDelivery = true; Audit(actor, source.TenantId, "shipping.shipment.split", source.Id, $"parts={result.Count}");
        await db.SaveChangesAsync(ct);
        var details = new List<ShipmentDetailDto>(); foreach (var shipmentId in result) details.Add(await DetailAsync(shipmentId, ct)); return details;
    }

    public async Task AssignAsync(Guid actor, Guid id, AssignCourierDto dto, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); EnsureStatus(shipment, "Ready", "Assigned", "Rescheduled");
        var driver = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == dto.DriverId && x.IsActive && x.IsPlatformStaff, ct)
            ?? throw ApiException.BadRequest("المندوب غير صالح");
        if (!driver.Roles.Any(x => x.Role.Code is "delivery_driver" or "super_admin")) throw ApiException.BadRequest("المستخدم ليس مندوب توصيل");
        shipment.DriverUserId = driver.Id; shipment.DriverName = driver.FullName; shipment.DriverPhone = driver.Phone;
        shipment.Status = "Assigned"; shipment.ScheduledAt = dto.ScheduledAt ?? shipment.ScheduledAt ?? DateTime.UtcNow;
        shipment.EstimatedArrival = dto.EstimatedArrival; shipment.UpdatedBy = actor;
        Event(shipment, "Assigned", $"تم تعيين المندوب {driver.FullName}"); Audit(actor, shipment.TenantId, "shipping.courier.assign", shipment.Id, driver.Id.ToString());
        await db.SaveChangesAsync(ct);
    }

    public async Task<ShippingRouteDto> CreateRouteAsync(Guid actor, CreateRouteDto dto, CancellationToken ct = default)
    {
        if (dto.ShipmentIds.Count == 0 || dto.ShipmentIds.Distinct().Count() != dto.ShipmentIds.Count) throw ApiException.BadRequest("اختر شحنة واحدة على الأقل");
        var driver = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == dto.DriverId && x.IsActive && x.IsPlatformStaff, ct) ?? throw ApiException.BadRequest("المندوب غير صالح");
        if (!driver.Roles.Any(x => x.Role.Code is "delivery_driver" or "super_admin")) throw ApiException.BadRequest("المستخدم ليس مندوب توصيل");
        var shipments = await db.Shipments.Where(x => dto.ShipmentIds.Contains(x.Id)).ToListAsync(ct);
        if (shipments.Count != dto.ShipmentIds.Count || shipments.Any(x => x.DriverUserId != dto.DriverId || !ActiveStatuses.Contains(x.Status)))
            throw ApiException.Conflict("كل الشحنات يجب أن تكون نشطة ومخصصة لنفس المندوب");
        if (await db.DeliveryRouteStops.AnyAsync(x => dto.ShipmentIds.Contains(x.ShipmentId) &&
            (x.Status == DeliveryStopStatus.Pending || x.Status == DeliveryStopStatus.InProgress), ct))
            throw ApiException.Conflict("إحدى الشحنات موجودة بالفعل في مسار نشط");
        var route = new DeliveryRoute { Code = $"RTE-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}",
            DriverUserId = driver.Id, DriverName = driver.FullName, RouteDate = dto.RouteDate.Date, OriginLatitude = dto.OriginLatitude,
            OriginLongitude = dto.OriginLongitude, CreatedBy = actor };
        db.DeliveryRoutes.Add(route);
        for (var i = 0; i < dto.ShipmentIds.Count; i++) db.DeliveryRouteStops.Add(new DeliveryRouteStop { RouteId = route.Id, ShipmentId = dto.ShipmentIds[i], Sequence = i + 1, ScheduledAt = shipments.First(x => x.Id == dto.ShipmentIds[i]).ScheduledAt, CreatedBy = actor });
        Audit(actor, Guid.Empty, "shipping.route.create", route.Id, route.Code); await db.SaveChangesAsync(ct);
        return (await RoutesAsync(ct)).First(x => x.Id == route.Id);
    }

    public async Task<ShippingRouteDto> OptimizeRouteAsync(Guid actor, Guid id, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var route = await db.DeliveryRoutes.Include(x => x.Stops).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("المسار غير موجود");
        if (route.Status is DeliveryRouteStatus.InProgress or DeliveryRouteStatus.Completed) throw ApiException.Conflict("لا يمكن تحسين مسار بدأ بالفعل");
        var ids = route.Stops.Select(x => x.ShipmentId).ToList(); var shipments = await db.Shipments.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var remaining = route.Stops.ToList(); var ordered = new List<DeliveryRouteStop>(); double? lat = route.OriginLatitude, lng = route.OriginLongitude; decimal distance = 0;
        while (remaining.Count > 0)
        {
            var next = lat is null || lng is null ? remaining[0] : remaining.OrderBy(x => Distance(lat.Value, lng.Value, shipments[x.ShipmentId].DestinationLatitude, shipments[x.ShipmentId].DestinationLongitude)).First();
            var s = shipments[next.ShipmentId]; var leg = Distance(lat, lng, s.DestinationLatitude, s.DestinationLongitude); distance += (decimal)leg;
            lat = s.DestinationLatitude ?? lat; lng = s.DestinationLongitude ?? lng; ordered.Add(next); remaining.Remove(next);
        }
        for (var i = 0; i < ordered.Count; i++) ordered[i].Sequence = -(i + 1);
        await db.SaveChangesAsync(ct);
        for (var i = 0; i < ordered.Count; i++) ordered[i].Sequence = i + 1;
        route.TotalDistanceKm = decimal.Round(distance, 2); route.EstimatedMinutes = Math.Max(ordered.Count * 12, (int)Math.Ceiling(distance / 30m * 60) + ordered.Count * 8);
        route.Status = DeliveryRouteStatus.Optimized; route.UpdatedBy = actor; Audit(actor, Guid.Empty, "shipping.route.optimize", route.Id, $"km={route.TotalDistanceKm}");
        await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); return (await RoutesAsync(ct)).First(x => x.Id == route.Id);
    }

    public async Task StartAsync(Guid actor, Guid id, double? latitude, double? longitude, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); EnsureStatus(shipment, "Assigned", "Rescheduled");
        if (shipment.DriverUserId is null) throw ApiException.Conflict("عيّن مندوبًا أولًا");
        shipment.Status = "OutForDelivery"; shipment.StartedAt = DateTime.UtcNow; shipment.DeliveryAttempt++;
        shipment.DriverLatitude = latitude ?? shipment.DriverLatitude; shipment.DriverLongitude = longitude ?? shipment.DriverLongitude; shipment.UpdatedBy = actor;
        shipment.Order.Status = OrderStatus.OutForDelivery; Event(shipment, "OutForDelivery", "بدأ المندوب رحلة التوصيل", latitude: latitude, longitude: longitude);
        await UpdateRouteStopAsync(shipment.Id, DeliveryStopStatus.InProgress, ct); Audit(actor, shipment.TenantId, "shipping.delivery.start", shipment.Id, null); await db.SaveChangesAsync(ct);
    }

    public async Task ContactAsync(Guid actor, Guid id, ContactCustomerDto dto, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); var channels = new[] { "Phone", "WhatsApp", "Sms" };
        if (!channels.Contains(dto.Channel)) throw ApiException.BadRequest("قناة التواصل غير صالحة");
        shipment.CustomerContactedAt = DateTime.UtcNow; shipment.CustomerContactChannel = dto.Channel; shipment.UpdatedBy = actor;
        Event(shipment, "CustomerContacted", $"تم التواصل مع المستلم عبر {dto.Channel}: {Clean(dto.Note, 250)}"); Audit(actor, shipment.TenantId, "shipping.customer.contact", id, dto.Channel); await db.SaveChangesAsync(ct);
    }

    public async Task<ShippingProofDto> AddProofAsync(Guid actor, Guid id, ShippingProofForm form, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); EnsureStatus(shipment, "OutForDelivery");
        if (!AllowedProofs.Contains(form.Type) || !Enum.TryParse<DeliveryProofType>(form.Type, true, out var type)) throw ApiException.BadRequest("نوع الإثبات غير صالح");
        if (form.File is null || form.File.Length == 0 || form.File.Length > 10 * 1024 * 1024) throw ApiException.BadRequest("الملف مطلوب وبحد أقصى 10MB");
        var allowed = new[] { "image/jpeg", "image/png", "application/pdf" }; if (!allowed.Contains(form.File.ContentType)) throw ApiException.BadRequest("صيغة الملف غير مدعومة");
        var ext = form.File.ContentType == "image/png" ? ".png" : form.File.ContentType == "application/pdf" ? ".pdf" : ".jpg";
        var relative = Path.Combine("shipping", shipment.TenantId.ToString("N"), shipment.Id.ToString("N"), $"{Guid.NewGuid():N}{ext}");
        var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data")); var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw ApiException.BadRequest("مسار الملف غير صالح");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); await using (var stream = File.Create(path)) await form.File.CopyToAsync(stream, ct);
        var proof = new DeliveryProof { TenantId = shipment.TenantId, OrderId = shipment.OrderId, ShipmentId = shipment.Id, Type = type,
            RecipientName = Clean(form.RecipientName, 150), Note = Clean(form.Note, 500), StoredPath = relative.Replace('\\', '/'), OriginalName = Path.GetFileName(form.File.FileName),
            ContentType = form.File.ContentType, Latitude = form.Latitude, Longitude = form.Longitude, CreatedBy = actor };
        db.DeliveryProofs.Add(proof); Event(shipment, "ProofCaptured", type == DeliveryProofType.Signature ? "تم حفظ التوقيع الإلكتروني" : "تم حفظ صورة إثبات التسليم", latitude: form.Latitude, longitude: form.Longitude);
        Audit(actor, shipment.TenantId, "shipping.proof.add", proof.Id, type.ToString()); await db.SaveChangesAsync(ct);
        return new(proof.Id, proof.Type.ToString(), proof.RecipientName, proof.Note, proof.Latitude, proof.Longitude, true, proof.CreatedAt);
    }

    public async Task ConfirmAsync(Guid actor, Guid id, string? recipientName, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); EnsureStatus(shipment, "OutForDelivery");
        if (!await db.DeliveryProofs.AnyAsync(x => x.ShipmentId == id && (x.Type == DeliveryProofType.Photo || x.Type == DeliveryProofType.Signature), ct))
            throw ApiException.Conflict("أضف صورة أو توقيعًا قبل تأكيد الاستلام");
        shipment.Status = "Delivered"; shipment.DeliveredAt = DateTime.UtcNow; shipment.UpdatedBy = actor;
        Event(shipment, "Delivered", $"تم التسليم إلى {Clean(recipientName, 150) ?? shipment.Order.ReceiverName}");
        await UpdateRouteStopAsync(id, DeliveryStopStatus.Delivered, ct);
        var pending = await db.Shipments.AnyAsync(x => x.OrderId == shipment.OrderId && x.Id != id && x.Status != "Delivered" && x.Status != "Cancelled", ct);
        shipment.Order.Status = pending ? OrderStatus.PartiallyDelivered : OrderStatus.Delivered;
        Audit(actor, shipment.TenantId, "shipping.delivery.confirm", id, recipientName); await db.SaveChangesAsync(ct); await CompleteRouteIfDoneAsync(id, ct);
    }

    public async Task FailAsync(Guid actor, Guid id, FailDeliveryDto dto, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); EnsureStatus(shipment, "OutForDelivery"); var reason = Clean(dto.Reason, 250) ?? throw ApiException.BadRequest("سبب الفشل مطلوب");
        shipment.Status = "Failed"; shipment.FailedAt = DateTime.UtcNow; shipment.FailureReason = reason; shipment.UpdatedBy = actor; shipment.Order.Status = OrderStatus.Delayed;
        Event(shipment, "Failed", $"فشل التسليم: {reason}. {Clean(dto.Note, 400)}", latitude: dto.Latitude, longitude: dto.Longitude);
        await UpdateRouteStopAsync(id, DeliveryStopStatus.Failed, ct); Audit(actor, shipment.TenantId, "shipping.delivery.fail", id, reason); await db.SaveChangesAsync(ct); await CompleteRouteIfDoneAsync(id, ct);
    }

    public async Task RescheduleAsync(Guid actor, Guid id, RescheduleDeliveryDto dto, CancellationToken ct = default)
    {
        var shipment = await ShipmentAsync(id, ct); EnsureStatus(shipment, "Failed"); if (dto.ScheduledAt <= DateTime.UtcNow.AddMinutes(30)) throw ApiException.BadRequest("موعد الإعادة يجب أن يكون في المستقبل");
        shipment.Status = "Rescheduled"; shipment.ScheduledAt = dto.ScheduledAt; shipment.RescheduledAt = DateTime.UtcNow; shipment.FailureReason = null; shipment.UpdatedBy = actor;
        Event(shipment, "Rescheduled", $"إعادة جدولة التوصيل إلى {dto.ScheduledAt:u}: {Clean(dto.Note, 300)}"); Audit(actor, shipment.TenantId, "shipping.delivery.reschedule", id, dto.ScheduledAt.ToString("O")); await db.SaveChangesAsync(ct);
    }

    public async Task<ShippingZoneDto> SaveZoneAsync(Guid actor, Guid? id, SaveShippingZoneDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Governorate) || dto.BaseFee < 0 || dto.FeePerKg < 0 || dto.FeePerKm < 0 || dto.EstimatedDays is < 1 or > 30)
            throw ApiException.BadRequest("بيانات منطقة التوصيل غير صالحة");
        var zone = id is null ? new DeliveryZone { CreatedBy = actor } : await db.DeliveryZones.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("المنطقة غير موجودة");
        if (id is null) db.DeliveryZones.Add(zone); zone.NameAr = dto.Name.Trim(); zone.Governorate = dto.Governorate.Trim(); zone.CitiesCsv = Clean(dto.Cities, 1000);
        zone.BaseFee = dto.BaseFee; zone.FeePerKg = dto.FeePerKg; zone.FeePerKm = dto.FeePerKm; zone.EstimatedDays = dto.EstimatedDays; zone.IsActive = dto.IsActive; zone.UpdatedBy = actor;
        Audit(actor, Guid.Empty, id is null ? "shipping.zone.create" : "shipping.zone.update", zone.Id, zone.NameAr); await db.SaveChangesAsync(ct);
        var count = await db.Shipments.CountAsync(x => x.DeliveryZone == zone.NameAr, ct); var revenue = await db.Shipments.Where(x => x.DeliveryZone == zone.NameAr).SumAsync(x => (decimal?)x.DeliveryCost, ct) ?? 0;
        return new(zone.Id, zone.NameAr, zone.Governorate, zone.CitiesCsv, zone.BaseFee, zone.FeePerKg, zone.FeePerKm, zone.EstimatedDays, zone.IsActive, count, revenue);
    }

    private IQueryable<ShipmentJoin> ShipmentRows() => from s in db.Shipments.AsNoTracking()
        join o in db.Orders.AsNoTracking() on s.OrderId equals o.Id
        join c in db.Companies.AsNoTracking() on o.TenantId equals c.TenantId
        select new ShipmentJoin { Shipment = s, Order = o, Company = c.LegalName,
            ItemCount = db.ShipmentItems.Count(i => i.ShipmentId == s.Id) };
    private static ShipmentRowDto MapRow(ShipmentJoin x) => new(x.Shipment.Id, x.Order.Id, x.Shipment.Number, x.Order.Number,
        x.Company, x.Shipment.Status, x.Order.DeliveryAddress, x.Order.ReceiverName, x.Order.ReceiverPhone,
        x.Shipment.DriverUserId, x.Shipment.DriverName, x.Shipment.DriverPhone, x.Shipment.DriverLatitude, x.Shipment.DriverLongitude,
        x.Shipment.DestinationLatitude, x.Shipment.DestinationLongitude, x.Shipment.DeliveryZone, x.Shipment.WeightKg,
        x.Shipment.DeliveryCost, x.Shipment.DeliveryAttempt, x.Shipment.ScheduledAt, x.Shipment.EstimatedArrival,
        x.Shipment.DeliveredAt, x.Shipment.FailureReason, x.ItemCount, x.Shipment.CreatedAt);
    private async Task<List<CourierDto>> CouriersAsync(List<ShipmentJoin> rows, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().Where(x => x.IsActive && x.IsPlatformStaff && x.Roles.Any(r => r.Role.Code == "delivery_driver" || r.Role.Code == "super_admin")).OrderBy(x => x.FullName).ToListAsync(ct);
        var ratings = await db.OrderRatings.AsNoTracking().ToListAsync(ct);
        return users.Select(u => { var mine = rows.Where(x => x.Shipment.DriverUserId == u.Id).ToList(); var delivered = mine.Where(x => x.Shipment.Status == "Delivered").ToList();
            var orderIds = delivered.Select(x => x.Order.Id).ToHashSet(); var score = ratings.Where(x => orderIds.Contains(x.OrderId)).Select(x => x.DeliveryRating).DefaultIfEmpty(0).Average();
            var latest = mine.OrderByDescending(x => x.Shipment.UpdatedAt ?? x.Shipment.CreatedAt).FirstOrDefault()?.Shipment;
            return new CourierDto(u.Id, u.FullName, u.Phone, u.AvatarPath, mine.Count(x => ActiveStatuses.Contains(x.Shipment.Status)), delivered.Count,
                mine.Count(x => x.Shipment.Status == "Failed"), Percent(delivered.Count, mine.Count),
                Percent(delivered.Count(x => x.Shipment.DeliveredAt <= x.Order.RequiredDate.Date.AddDays(1)), delivered.Count), decimal.Round((decimal)score, 1), latest?.DriverLatitude, latest?.DriverLongitude); }).ToList();
    }
    private async Task<List<ShippingRouteDto>> RoutesAsync(CancellationToken ct)
    {
        var routes = await db.DeliveryRoutes.AsNoTracking().Include(x => x.Stops).OrderByDescending(x => x.RouteDate).Take(100).ToListAsync(ct);
        var ids = routes.SelectMany(x => x.Stops).Select(x => x.ShipmentId).Distinct().ToList();
        var shipmentRows = await db.Shipments.AsNoTracking().Where(x => ids.Contains(x.Id)).Include(x => x.Order).ToDictionaryAsync(x => x.Id, ct);
        return routes.Select(r => new ShippingRouteDto(r.Id, r.Code, r.DriverUserId, r.DriverName, r.RouteDate, r.Status.ToString(), r.TotalDistanceKm,
            r.EstimatedMinutes, r.StartedAt, r.CompletedAt, r.Stops.OrderBy(x => x.Sequence).Where(x => shipmentRows.ContainsKey(x.ShipmentId)).Select(x => {
                var s = shipmentRows[x.ShipmentId]; return new RouteStopDto(x.Id, s.Id, s.Number, s.Order.DeliveryAddress, x.Sequence, x.Status.ToString(), x.ScheduledAt, s.DestinationLatitude, s.DestinationLongitude); }).ToList())).ToList();
    }
    private async Task<Shipment> ShipmentAsync(Guid id, CancellationToken ct) => await db.Shipments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الشحنة غير موجودة");
    private static void EnsureStatus(Shipment shipment, params string[] allowed) { if (!allowed.Contains(shipment.Status)) throw ApiException.Conflict("حالة الشحنة لا تسمح بهذه العملية"); }
    private static void ValidateAllocation(ICollection<OrderItem> orderItems, IReadOnlyList<ShippingItemInputDto> inputs, Dictionary<Guid, int> allocated)
    {
        if (inputs.Count == 0 || inputs.Any(x => x.Quantity <= 0) || inputs.Select(x => x.OrderItemId).Distinct().Count() != inputs.Count) throw ApiException.BadRequest("كميات الشحنة غير صالحة");
        foreach (var input in inputs) { var item = orderItems.FirstOrDefault(x => x.Id == input.OrderItemId) ?? throw ApiException.BadRequest("صنف غير موجود في الطلب"); if (allocated.GetValueOrDefault(item.Id) + input.Quantity > item.Quantity) throw ApiException.Conflict("الكمية تتجاوز المتبقي في الطلب"); }
    }
    private void Event(Shipment shipment, string status, string description, string? location = null, double? latitude = null, double? longitude = null) => db.ShipmentEvents.Add(new ShipmentEvent { TenantId = shipment.TenantId, ShipmentId = shipment.Id, Status = status, DescriptionAr = description, Location = location, Latitude = latitude, Longitude = longitude });
    private void Audit(Guid actor, Guid tenant, string action, Guid id, string? data) => db.AuditLogs.Add(new AuditLog { UserId = actor, TenantId = tenant == Guid.Empty ? null : tenant, Action = action, EntityType = nameof(Shipment), EntityId = id.ToString(), DataJson = data });
    private async Task UpdateRouteStopAsync(Guid shipmentId, DeliveryStopStatus status, CancellationToken ct) { var stop = await db.DeliveryRouteStops.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, ct); if (stop is null) return; stop.Status = status; if (status == DeliveryStopStatus.InProgress) { stop.ArrivedAt = DateTime.UtcNow; var route = await db.DeliveryRoutes.FirstAsync(x => x.Id == stop.RouteId, ct); if (route.Status is DeliveryRouteStatus.Planned or DeliveryRouteStatus.Optimized) { route.Status = DeliveryRouteStatus.InProgress; route.StartedAt = DateTime.UtcNow; } } if (status is DeliveryStopStatus.Delivered or DeliveryStopStatus.Failed) stop.CompletedAt = DateTime.UtcNow; }
    private async Task CompleteRouteIfDoneAsync(Guid shipmentId, CancellationToken ct) { var stop = await db.DeliveryRouteStops.AsNoTracking().OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(x => x.ShipmentId == shipmentId, ct); if (stop is null) return; var route = await db.DeliveryRoutes.Include(x => x.Stops).FirstAsync(x => x.Id == stop.RouteId, ct); if (route.Stops.All(x => x.Status is DeliveryStopStatus.Delivered or DeliveryStopStatus.Failed or DeliveryStopStatus.Skipped)) { route.Status = DeliveryRouteStatus.Completed; route.CompletedAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); } }
    private async Task EnsureDefaultZonesAsync(CancellationToken ct) { if (await db.DeliveryZones.AnyAsync(ct)) return; db.DeliveryZones.AddRange(
        new DeliveryZone { NameAr = "القاهرة الكبرى", Governorate = "القاهرة", CitiesCsv = "القاهرة,الجيزة,القليوبية", BaseFee = 75, FeePerKg = 4, FeePerKm = 1.5m, EstimatedDays = 1 },
        new DeliveryZone { NameAr = "الدلتا", Governorate = "الدقهلية", CitiesCsv = "المنصورة,طنطا,الزقازيق,دمياط", BaseFee = 110, FeePerKg = 5, FeePerKm = 1.75m, EstimatedDays = 2 },
        new DeliveryZone { NameAr = "الإسكندرية والساحل", Governorate = "الإسكندرية", CitiesCsv = "الإسكندرية,العلمين,مرسى مطروح", BaseFee = 125, FeePerKg = 5.5m, FeePerKm = 2, EstimatedDays = 2 },
        new DeliveryZone { NameAr = "الصعيد", Governorate = "أسيوط", CitiesCsv = "الفيوم,بني سويف,المنيا,أسيوط,سوهاج,قنا,أسوان", BaseFee = 170, FeePerKg = 7, FeePerKm = 2.25m, EstimatedDays = 4 }); await db.SaveChangesAsync(ct); }
    private static decimal Percent(int value, int total) => total == 0 ? 0 : decimal.Round(value * 100m / total, 1);
    private static string Number() => $"SHP-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var clean = value.Trim(); return clean.Length <= max ? clean : clean[..max]; }
    private static double Distance(double? lat1, double? lng1, double? lat2, double? lng2) { if (lat1 is null || lng1 is null || lat2 is null || lng2 is null) return 0; const double radius = 6371; var dLat = (lat2.Value - lat1.Value) * Math.PI / 180; var dLng = (lng2.Value - lng1.Value) * Math.PI / 180; var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1.Value * Math.PI / 180) * Math.Cos(lat2.Value * Math.PI / 180) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2); return radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)); }
    private sealed class ShipmentJoin
    {
        public Shipment Shipment { get; init; } = null!;
        public Order Order { get; init; } = null!;
        public string Company { get; init; } = string.Empty;
        public int ItemCount { get; init; }
    }
}
