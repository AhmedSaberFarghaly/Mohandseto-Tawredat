using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Approvals;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Rfq;

public sealed class RfqService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment environment,
    ApprovalService? approvals = null, FinanceService? finance = null)
{
    private static readonly Dictionary<string, (RfqAttachmentType Type, string Ext)> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = (RfqAttachmentType.Excel, ".xlsx"),
        ["application/vnd.ms-excel"] = (RfqAttachmentType.Excel, ".xls"), ["text/csv"] = (RfqAttachmentType.Excel, ".csv"),
        ["application/pdf"] = (RfqAttachmentType.Pdf, ".pdf"), ["image/png"] = (RfqAttachmentType.Image, ".png"),
        ["image/jpeg"] = (RfqAttachmentType.Image, ".jpg"),
    };

    public async Task<List<RfqListDto>> ListAsync(Guid userId, string? status, CancellationToken ct = default)
    {
        var query = db.Rfqs.AsNoTracking().Include(r => r.Items).Include(r => r.Quotes).ThenInclude(q => q.Versions)
            .Where(r => r.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RfqStatus>(status, true, out var parsed)) query = query.Where(r => r.Status == parsed);
        var data = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return data.Select(r => new RfqListDto(r.Id, r.Number, r.Title, EffectiveStatus(r).ToString(), r.Items.Count,
            r.RequiredDate, r.QuoteDeadline, r.Quotes.SelectMany(q => q.Versions).OrderByDescending(v => v.VersionNumber).Select(v => (decimal?)v.Total).FirstOrDefault(), r.CreatedAt)).ToList();
    }

    public async Task<RfqDetailDto> CreateAsync(Guid userId, CreateRfqDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Title) || dto.RequiredDate.Date < DateTime.UtcNow.Date.AddDays(1) ||
            dto.QuoteDeadline <= DateTime.UtcNow || dto.QuoteDeadline >= dto.RequiredDate)
            throw ApiException.BadRequest("بيانات طلب عرض السعر أو التواريخ غير صالحة");
        var rfq = new Domain.Entities.Rfq { TenantId = TenantId(), UserId = userId,
            Number = $"RFQ-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Title = CleanRequired(dto.Title, 200), Description = Clean(dto.Description, 2000), RequiredDate = dto.RequiredDate.Date,
            QuoteDeadline = dto.QuoteDeadline, DeliveryGovernorate = Clean(dto.DeliveryGovernorate, 100) };
        db.Rfqs.Add(rfq); await db.SaveChangesAsync(ct); return await DetailAsync(userId, rfq.Id, ct);
    }

    public async Task<RfqDetailDto> AddItemAsync(Guid userId, Guid id, UpsertRfqItemDto dto, CancellationToken ct = default)
    {
        var rfq = await EditableAsync(userId, id, ct); var source = Enum.TryParse<RfqItemSource>(dto.Source, true, out var parsed) ? parsed : RfqItemSource.FreeText;
        if (dto.Quantity <= 0 || string.IsNullOrWhiteSpace(dto.Description)) throw ApiException.BadRequest("بيانات الصنف غير مكتملة");
        Product? product = null; if (dto.ProductId is { } productId) product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId && p.Status == ProductStatus.Active, ct)
            ?? throw ApiException.BadRequest("المنتج المختار غير صالح");
        db.RfqItems.Add(new RfqItem { TenantId = TenantId(), RfqId = rfq.Id, ProductId = product?.Id, DescriptionAr = product?.NameAr ?? CleanRequired(dto.Description, 300),
            SkuHint = product?.Sku, Quantity = dto.Quantity, UnitName = CleanRequired(dto.UnitName, 50), Specifications = Clean(dto.Specifications, 1500),
            PreferredBrand = Clean(dto.PreferredBrand, 100), AllowAlternatives = dto.AllowAlternatives, Source = source, IsReviewed = dto.IsReviewed });
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<RfqDetailDto> UpdateItemAsync(Guid userId, Guid id, Guid itemId, UpsertRfqItemDto dto, CancellationToken ct = default)
    {
        var rfq = await EditableAsync(userId, id, ct); var item = rfq.Items.FirstOrDefault(i => i.Id == itemId) ?? throw ApiException.NotFound("الصنف غير موجود");
        if (dto.Quantity <= 0 || string.IsNullOrWhiteSpace(dto.Description)) throw ApiException.BadRequest("بيانات الصنف غير مكتملة");
        if (dto.ProductId is { } productId && !await db.Products.AnyAsync(p => p.Id == productId && p.Status == ProductStatus.Active, ct)) throw ApiException.BadRequest("المنتج المختار غير صالح");
        item.ProductId = dto.ProductId; item.DescriptionAr = CleanRequired(dto.Description, 300); item.Quantity = dto.Quantity;
        item.UnitName = CleanRequired(dto.UnitName, 50); item.Specifications = Clean(dto.Specifications, 1500);
        item.PreferredBrand = Clean(dto.PreferredBrand, 100); item.AllowAlternatives = dto.AllowAlternatives; item.IsReviewed = true;
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<RfqDetailDto> RemoveItemAsync(Guid userId, Guid id, Guid itemId, CancellationToken ct = default)
    {
        var rfq = await EditableAsync(userId, id, ct); var item = rfq.Items.FirstOrDefault(i => i.Id == itemId) ?? throw ApiException.NotFound("الصنف غير موجود");
        db.RfqItems.Remove(item); await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<RfqAttachmentDto> UploadAsync(Guid userId, Guid id, IFormFile file, CancellationToken ct = default)
    {
        var rfq = await EditableAsync(userId, id, ct);
        if (file.Length is <= 0 or > 15 * 1024 * 1024 || !AllowedFiles.TryGetValue(file.ContentType, out var allowed))
            throw ApiException.BadRequest("الملف يجب أن يكون Excel أو CSV أو PDF أو صورة وبحجم أقصى 15MB");
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "rfq", TenantId().ToString("N"), rfq.Id.ToString("N"));
        Directory.CreateDirectory(folder); var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{allowed.Ext}");
        await using (var stream = File.Create(absolute)) await file.CopyToAsync(stream, ct);
        var attachment = new RfqAttachment { TenantId = TenantId(), RfqId = rfq.Id, Type = allowed.Type,
            OriginalName = Path.GetFileName(file.FileName), StoredPath = Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/'),
            ContentType = file.ContentType, SizeBytes = file.Length, ExtractionStatus = "Processing" };
        db.RfqAttachments.Add(attachment); await db.SaveChangesAsync(ct);
        try
        {
            var extracted = await ExtractAsync(absolute, allowed.Type, attachment.OriginalName, ct);
            foreach (var row in extracted) db.RfqItems.Add(new RfqItem { TenantId = TenantId(), RfqId = rfq.Id, DescriptionAr = row.Description,
                Quantity = row.Quantity, UnitName = row.Unit, Source = allowed.Type switch { RfqAttachmentType.Excel => RfqItemSource.Excel,
                    RfqAttachmentType.Pdf => RfqItemSource.Pdf, _ => RfqItemSource.Image }, ExtractionConfidence = row.Confidence, IsReviewed = false });
            attachment.ExtractionStatus = "Completed"; rfq.Status = RfqStatus.NeedsReview;
        }
        catch (Exception error) { attachment.ExtractionStatus = "Failed"; attachment.ExtractionError = Clean(error.Message, 500); }
        await db.SaveChangesAsync(ct); return Attachment(attachment);
    }

    public async Task<(string Path, string Type, string Name)> FileAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var file = await db.RfqAttachments.AsNoTracking().Include(a => a.Rfq).FirstOrDefaultAsync(a => a.Id == id && a.Rfq.UserId == userId, ct)
            ?? throw ApiException.NotFound("المرفق غير موجود"); return (SafePath(file.StoredPath), file.ContentType, file.OriginalName);
    }

    public async Task<RfqDetailDto> SubmitAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var rfq = await EditableAsync(userId, id, ct);
        if (rfq.Items.Count == 0) throw ApiException.BadRequest("أضف صنفًا واحدًا على الأقل");
        if (rfq.Items.Any(i => !i.IsReviewed)) throw ApiException.Conflict("راجع جميع العناصر المستخرجة قبل الإرسال");
        rfq.Status = RfqStatus.UnderReview; await db.SaveChangesAsync(ct);
        db.Notifications.Add(new AppNotification { TenantId = TenantId(), UserId = userId, Type = "rfq.submitted", Title = "تم إرسال طلب عرض السعر",
            Body = $"بدأت مراجعة {rfq.Number}", EntityType = nameof(Domain.Entities.Rfq), EntityId = rfq.Id });
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<RfqDetailDto> PublishQuoteAsync(Guid id, PublishQuoteDto dto, Guid staffUserId, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.Include(r => r.Items).Include(r => r.Quotes).ThenInclude(q => q.Versions).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw ApiException.NotFound("طلب عرض السعر غير موجود");
        if (dto.Items.Count != rfq.Items.Count || dto.ValidDays is < 1 or > 90 || dto.DeliveryDays <= 0) throw ApiException.BadRequest("بيانات العرض غير مكتملة");
        var quote = rfq.Quotes.FirstOrDefault();
        if (quote is null)
        {
            quote = new CustomerQuote { TenantId = rfq.TenantId, RfqId = rfq.Id,
                Number = $"QT-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}" };
            db.CustomerQuotes.Add(quote);
        }
        var versionNo = quote.CurrentVersion + 1; var version = new CustomerQuoteVersion { TenantId = rfq.TenantId,
            VersionNumber = versionNo, ValidUntil = DateTime.UtcNow.AddDays(dto.ValidDays), DeliveryDays = dto.DeliveryDays,
            Terms = Clean(dto.Terms, 2000), ChangeSummary = Clean(dto.ChangeSummary, 1000), SentAt = DateTime.UtcNow, Shipping = dto.Shipping };
        var quoteItems = new List<CustomerQuoteItem>();
        foreach (var input in dto.Items)
        {
            var source = rfq.Items.FirstOrDefault(i => i.Id == input.RfqItemId) ?? throw ApiException.BadRequest("صنف العرض غير صالح");
            quoteItems.Add(new CustomerQuoteItem { TenantId = rfq.TenantId, VersionId = version.Id, RfqItemId = source.Id, ProductId = input.ProductId ?? source.ProductId,
                DescriptionAr = source.DescriptionAr, Quantity = source.Quantity, UnitName = source.UnitName, UnitPrice = input.UnitPrice,
                LineTotal = decimal.Round(input.UnitPrice * source.Quantity, 2), IsAlternative = input.IsAlternative,
                AlternativeReason = Clean(input.AlternativeReason, 500) });
        }
        version.Subtotal = quoteItems.Sum(i => i.LineTotal); version.Tax = decimal.Round(version.Subtotal * .14m, 2); version.Total = version.Subtotal + version.Tax + version.Shipping;
        version.QuoteId = quote.Id; db.CustomerQuoteVersions.Add(version); db.CustomerQuoteItems.AddRange(quoteItems);
        quote.CurrentVersion = versionNo; quote.Status = CustomerQuoteStatus.Sent; rfq.Status = RfqStatus.Quoted;
        db.Notifications.Add(new AppNotification { TenantId = rfq.TenantId, UserId = rfq.UserId, Type = "rfq.quoted", Title = "وصل عرض السعر",
            Body = $"العرض {quote.Number} بقيمة {version.Total:0.##} ج.م", EntityType = nameof(Domain.Entities.Rfq), EntityId = rfq.Id });
        await db.SaveChangesAsync(ct); return await DetailAsync(rfq.UserId, rfq.Id, ct);
    }

    public async Task<RfqDetailDto> NegotiateAsync(Guid userId, Guid id, NegotiationRequestDto dto, CancellationToken ct = default)
    {
        var rfq = await OwnedAsync(userId, id, ct); if (rfq.Status is not (RfqStatus.Quoted or RfqStatus.Negotiating or RfqStatus.ClarificationRequested)) throw ApiException.Conflict("لا يمكن التفاوض على الطلب حاليًا");
        if (!Enum.TryParse<NegotiationMessageType>(dto.Type, true, out var type)) type = dto.ProposedTotal is null ? NegotiationMessageType.Message : NegotiationMessageType.CounterOffer;
        db.QuoteNegotiations.Add(new QuoteNegotiation { TenantId = TenantId(), RfqId = rfq.Id, UserId = userId, Type = type,
            Message = CleanRequired(dto.Message, 2000), ProposedTotal = dto.ProposedTotal }); rfq.Status = RfqStatus.Negotiating;
        var quote = await db.CustomerQuotes.FirstOrDefaultAsync(q => q.RfqId == rfq.Id, ct); if (quote is not null) quote.Status = CustomerQuoteStatus.RevisionRequested;
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<RfqDetailDto> QuoteDecisionAsync(Guid userId, Guid id, string decision, QuoteDecisionDto dto, CancellationToken ct = default)
    {
        var rfq = await OwnedAsync(userId, id, ct); var quote = await db.CustomerQuotes.Include(q => q.Versions).FirstOrDefaultAsync(q => q.RfqId == id, ct)
            ?? throw ApiException.NotFound("عرض السعر غير موجود"); var version = quote.Versions.FirstOrDefault(v => v.Id == dto.VersionId) ?? throw ApiException.BadRequest("نسخة العرض غير صالحة");
        if (version.ValidUntil < DateTime.UtcNow) { quote.Status = CustomerQuoteStatus.Expired; rfq.Status = RfqStatus.Expired; await db.SaveChangesAsync(ct); throw ApiException.Conflict("انتهت صلاحية عرض السعر؛ اطلب إعادة التسعير"); }
        switch (decision.ToLowerInvariant())
        {
            case "accept": quote.Status = CustomerQuoteStatus.Accepted; quote.AcceptedVersionId = version.Id; rfq.AcceptedQuoteId = quote.Id; rfq.Status = RfqStatus.Accepted; break;
            case "reject": quote.Status = CustomerQuoteStatus.Rejected; rfq.Status = RfqStatus.Rejected; break;
            case "requote": quote.Status = CustomerQuoteStatus.RevisionRequested; rfq.Status = RfqStatus.Negotiating;
                db.QuoteNegotiations.Add(new QuoteNegotiation { TenantId = TenantId(), RfqId = rfq.Id, UserId = userId,
                    Type = NegotiationMessageType.RevisionRequest, Message = Clean(dto.Comment, 2000) ?? "طلب إعادة تسعير" }); break;
            default: throw ApiException.BadRequest("قرار العرض غير صالح");
        }
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<ConvertedRfqOrderDto> ConvertAsync(Guid userId, Guid id, ConvertRfqDto dto, CancellationToken ct = default)
    {
        var rfq = await OwnedAsync(userId, id, ct); if (rfq.Status != RfqStatus.Accepted || rfq.AcceptedQuoteId is null) throw ApiException.Conflict("يجب قبول عرض سعر صالح أولًا");
        var quote = await db.CustomerQuotes.Include(q => q.Versions).ThenInclude(v => v.Items).FirstAsync(q => q.Id == rfq.AcceptedQuoteId, ct);
        var version = quote.Versions.First(v => v.Id == quote.AcceptedVersionId); if (version.Items.Any(i => i.ProductId == null)) throw ApiException.Conflict("يجب ربط كل أصناف العرض بمنتجات الكتالوج قبل التحويل");
        var branch = await db.CompanyBranches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == dto.BranchId, ct) ?? throw ApiException.BadRequest("عنوان التوصيل غير صالح");
        var center = await db.CostCenters.FirstOrDefaultAsync(c => c.Id == dto.CostCenterId && c.IsActive, ct) ?? throw ApiException.BadRequest("مركز التكلفة غير صالح");
        var available = center.BudgetAmount - center.UsedAmount - center.ReservedAmount; var requiresApproval = version.Total > available || version.Total >= (center.ApprovalThreshold ?? 5000);
        var order = new Order { TenantId = TenantId(), Number = $"ORD-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}", UserId = userId,
            BranchId = branch.Id, BranchName = branch.Name, DeliveryAddress = string.Join(" - ", new[] { branch.Governorate, branch.City, branch.AddressLine }.Where(x => !string.IsNullOrWhiteSpace(x))),
            ReceiverName = CleanRequired(dto.ReceiverName, 150), ReceiverPhone = CleanRequired(dto.ReceiverPhone, 30), RequiredDate = rfq.RequiredDate,
            ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.BankTransfer, CostCenterId = center.Id, CostCenterCode = center.Code,
            CostCenterName = center.NameAr, RequestingDepartment = "طلب عرض أسعار", SourceRfqId = rfq.Id, Status = requiresApproval ? OrderStatus.PendingApproval : OrderStatus.Confirmed,
            RequiresApproval = requiresApproval, Subtotal = version.Subtotal, TaxIncluded = version.Tax, Shipping = version.Shipping, Total = version.Total };
        foreach (var item in version.Items) order.Items.Add(new OrderItem { TenantId = TenantId(), ProductId = item.ProductId!.Value,
            Sku = await db.Products.Where(p => p.Id == item.ProductId).Select(p => p.Sku).FirstAsync(ct), NameAr = item.DescriptionAr,
            Quantity = decimal.ToInt32(item.Quantity), UnitPrice = item.UnitPrice, LineTotal = item.LineTotal });
        order.History.Add(new OrderStatusHistory { TenantId = TenantId(), Status = order.Status, ChangedBy = userId, Note = $"تحويل من {rfq.Number}" });
        db.Orders.Add(order); finance?.IssueForOrder(order); if (requiresApproval) { center.ReservedAmount += order.Total; if (approvals is not null) await approvals.CreateForOrderAsync(order, version.Total > available, userId, ct); }
        else center.UsedAmount += order.Total;
        rfq.Status = RfqStatus.Converted; rfq.ConvertedOrderId = order.Id; await db.SaveChangesAsync(ct);
        return new(order.Id, order.Number, order.Status.ToString(), order.RequiresApproval, order.Total);
    }

    public async Task<RfqConversionOptionsDto> ConversionOptionsAsync(CancellationToken ct = default)
    {
        var tenantId = TenantId();
        var branches = await db.CompanyBranches.AsNoTracking().OrderByDescending(b => b.IsMain).Select(b => new RfqBranchDto(b.Id, b.Name,
            string.Join(" - ", new[] { b.Governorate, b.City, b.AddressLine }.Where(x => !string.IsNullOrWhiteSpace(x))))).ToListAsync(ct);
        var centers = await db.CostCenters.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Code).Select(c => new RfqCostCenterDto(c.Id, c.Code, c.NameAr,
            c.BudgetAmount - c.UsedAmount - c.ReservedAmount)).ToListAsync(ct);
        var receivers = await db.Users.AsNoTracking().Where(u => u.TenantId == tenantId && u.IsActive).OrderBy(u => u.FullName)
            .Select(u => new RfqReceiverDto(u.Id, u.FullName, u.Phone)).ToListAsync(ct);
        return new(branches, centers, receivers);
    }

    public async Task<byte[]> QuotePdfAsync(Guid userId, Guid id, Guid versionId, CancellationToken ct = default)
    {
        var detail = await DetailAsync(userId, id, ct); var version = detail.QuoteVersions.FirstOrDefault(v => v.Id == versionId) ?? throw ApiException.NotFound("نسخة العرض غير موجودة");
        var lines = new List<string> { $"Quote {detail.QuoteNumber} - Version {version.Version}", $"RFQ {detail.Number}", $"Total EGP {version.Total:0.00}", $"Valid until {version.ValidUntil:yyyy-MM-dd}" };
        lines.AddRange(version.Items.Select(i => $"{i.Description} | {i.Quantity} {i.UnitName} | {i.LineTotal:0.00}")); return SimplePdf(lines);
    }

    public async Task<RfqDetailDto> DetailAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.AsNoTracking().Include(r => r.Items).Include(r => r.Attachments).Include(r => r.Negotiations)
            .Include(r => r.Quotes).ThenInclude(q => q.Versions).ThenInclude(v => v.Items).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
            ?? throw ApiException.NotFound("طلب عرض السعر غير موجود"); var quote = rfq.Quotes.FirstOrDefault();
        return new(rfq.Id, rfq.Number, rfq.Title, rfq.Description, EffectiveStatus(rfq).ToString(), rfq.RequiredDate, rfq.QuoteDeadline,
            rfq.DeliveryGovernorate, rfq.Items.OrderBy(i => i.CreatedAt).Select(Item).ToList(), rfq.Attachments.OrderByDescending(a => a.CreatedAt).Select(Attachment).ToList(),
            quote?.Number, quote?.Status.ToString(), quote?.Versions.OrderByDescending(v => v.VersionNumber).Select(Version).ToList() ?? [],
            rfq.Negotiations.OrderBy(n => n.CreatedAt).Select(n => new NegotiationDto(n.Id, n.UserId, n.IsStaff, n.Type.ToString(), n.Message, n.ProposedTotal, n.CreatedAt)).ToList(), rfq.ConvertedOrderId);
    }

    private async Task<Domain.Entities.Rfq> EditableAsync(Guid userId, Guid id, CancellationToken ct) { var rfq = await db.Rfqs.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
        ?? throw ApiException.NotFound("طلب عرض السعر غير موجود"); if (rfq.Status is not (RfqStatus.Draft or RfqStatus.NeedsReview)) throw ApiException.Conflict("لا يمكن تعديل الطلب في حالته الحالية"); return rfq; }
    private async Task<Domain.Entities.Rfq> OwnedAsync(Guid userId, Guid id, CancellationToken ct) => await db.Rfqs.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct) ?? throw ApiException.NotFound("طلب عرض السعر غير موجود");
    private static RfqItemDto Item(RfqItem i) => new(i.Id, i.ProductId, i.DescriptionAr, i.SkuHint, i.Quantity, i.UnitName, i.Specifications, i.PreferredBrand, i.AllowAlternatives, i.Source.ToString(), i.ExtractionConfidence, i.IsReviewed);
    private static RfqAttachmentDto Attachment(RfqAttachment a) => new(a.Id, a.OriginalName, a.Type.ToString(), a.SizeBytes, a.ExtractionStatus, a.ExtractionError, $"/api/rfqs/attachments/{a.Id}");
    private static QuoteVersionDto Version(CustomerQuoteVersion v) => new(v.Id, v.VersionNumber, v.Subtotal, v.Tax, v.Shipping, v.Total, v.ValidUntil,
        v.DeliveryDays, v.Terms, v.ChangeSummary, v.SentAt, v.Items.Select(i => new QuoteItemDto(i.Id, i.RfqItemId, i.ProductId, i.DescriptionAr, i.Quantity, i.UnitName, i.UnitPrice, i.LineTotal, i.IsAlternative, i.AlternativeReason)).ToList(), v.ValidUntil < DateTime.UtcNow);
    private static RfqStatus EffectiveStatus(Domain.Entities.Rfq r) => r.Status == RfqStatus.Quoted && r.Quotes.SelectMany(q => q.Versions).Any() && r.Quotes.SelectMany(q => q.Versions).Max(v => v.ValidUntil) < DateTime.UtcNow ? RfqStatus.Expired : r.Status;

    private async Task<List<(string Description, decimal Quantity, string Unit, decimal Confidence)>> ExtractAsync(string path, RfqAttachmentType type, string name, CancellationToken ct)
    {
        if (type == RfqAttachmentType.Excel && Path.GetExtension(path).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return ExtractXlsx(path);
        if (type == RfqAttachmentType.Excel && Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        { var rows = await File.ReadAllLinesAsync(path, ct); return rows.Skip(1).Select(ParseDelimited).Where(x => x.Description.Length > 0).ToList(); }
        return [(Path.GetFileNameWithoutExtension(name).Replace('_', ' '), 1, "قطعة", .55m)];
    }
    private static List<(string Description, decimal Quantity, string Unit, decimal Confidence)> ExtractXlsx(string path)
    {
        using var zip = ZipFile.OpenRead(path); XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sharedEntry = zip.GetEntry("xl/sharedStrings.xml"); var shared = new List<string>(); if (sharedEntry is not null) { using var s = sharedEntry.Open();
            shared = XDocument.Load(s).Descendants(ns + "si").Select(x => string.Concat(x.Descendants(ns + "t").Select(t => t.Value))).ToList(); }
        var sheet = zip.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidOperationException("ورقة Excel الأولى غير موجودة"); using var stream = sheet.Open();
        var rows = XDocument.Load(stream).Descendants(ns + "row").Skip(1); var result = new List<(string, decimal, string, decimal)>();
        foreach (var row in rows) { var values = row.Elements(ns + "c").Select(c => { var raw = c.Element(ns + "v")?.Value ?? "";
            return c.Attribute("t")?.Value == "s" && int.TryParse(raw, out var index) && index < shared.Count ? shared[index] : raw; }).ToList();
            var parsed = ParseValues(values); if (parsed.Description.Length > 0) result.Add(parsed); } return result;
    }
    private static (string Description, decimal Quantity, string Unit, decimal Confidence) ParseDelimited(string row) => ParseValues(row.Split([',',';','\t']).Select(x => x.Trim()).ToList());
    private static (string Description, decimal Quantity, string Unit, decimal Confidence) ParseValues(IReadOnlyList<string> values) =>
        (values.ElementAtOrDefault(0) ?? "", decimal.TryParse(values.ElementAtOrDefault(1), out var q) && q > 0 ? q : 1, values.ElementAtOrDefault(2) ?? "قطعة", .9m);
    private string SafePath(string relative) { var root = Path.GetFullPath(environment.ContentRootPath) + Path.DirectorySeparatorChar; var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw ApiException.NotFound("المرفق غير موجود"); return path; }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static string CleanRequired(string? value, int max) => string.IsNullOrWhiteSpace(value) ? throw ApiException.BadRequest("البيان المطلوب غير مكتمل") : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static byte[] SimplePdf(IEnumerable<string> lines)
    {
        var text = string.Join("\\n", lines.Select(l => l.Replace("(", "[").Replace(")", "]"))); var content = $"BT /F1 10 Tf 40 780 Td ({text.Replace("\\n", ") Tj 0 -16 Td (")}) Tj ET";
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>", "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream" };
        var sb = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int>(); for (var i=0;i<objects.Length;i++){offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));sb.Append($"{i+1} 0 obj\n{objects[i]}\nendobj\n");}
        var xref=Encoding.ASCII.GetByteCount(sb.ToString()); sb.Append($"xref\n0 {objects.Length+1}\n0000000000 65535 f \n"); foreach(var o in offsets) sb.Append($"{o:0000000000} 00000 n \n"); sb.Append($"trailer << /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
