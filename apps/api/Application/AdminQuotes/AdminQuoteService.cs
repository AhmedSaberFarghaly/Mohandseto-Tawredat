using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Rfq;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;
using RfqEntity = Mohandseto.Api.Domain.Entities.Rfq;

namespace Mohandseto.Api.Application.AdminQuotes;

public sealed class AdminQuoteService(AppDbContext db, RfqService rfqService)
{
    public async Task<AdminQuotePageDto> ListAsync(string? search, string? statuses, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = from rfq in db.Rfqs.AsNoTracking()
                    join company in db.Companies.AsNoTracking() on rfq.TenantId equals company.TenantId
                    join customer in db.Users.AsNoTracking() on rfq.UserId equals customer.Id
                    select new { rfq, company, customer };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.rfq.Number.Contains(term) || x.rfq.Title.Contains(term) ||
                x.company.LegalName.Contains(term) || x.customer.FullName.Contains(term));
        }
        var parsed = (statuses ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Enum.TryParse<RfqStatus>(x, true, out var value) ? value : (RfqStatus?)null)
            .Where(x => x is not null).Select(x => x!.Value).ToList();
        if (parsed.Count > 0) query = query.Where(x => parsed.Contains(x.rfq.Status));
        var total = await query.CountAsync(ct);
        var data = await query.OrderByDescending(x => x.rfq.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.rfq.Id, x.rfq.Number, x.rfq.Title, Company = x.company.LegalName, Customer = x.customer.FullName,
                x.rfq.Status, ItemCount = x.rfq.Items.Count, x.rfq.QuoteDeadline, x.rfq.CreatedAt,
                Versions = x.rfq.Quotes.SelectMany(q => q.Versions).OrderByDescending(v => v.VersionNumber)
                    .Select(v => new { v.Total, v.ValidUntil }).ToList()
            }).ToListAsync(ct);
        var now = DateTime.UtcNow;
        var baseQuery = db.Rfqs.AsNoTracking();
        var summary = new AdminQuoteSummaryDto(
            await baseQuery.CountAsync(ct),
            await baseQuery.CountAsync(x => x.Status == RfqStatus.NeedsReview || x.Status == RfqStatus.UnderReview, ct),
            await baseQuery.CountAsync(x => x.Status == RfqStatus.UnderReview && db.SupplierQuoteRequests.Any(r => r.RfqId == x.Id), ct),
            await baseQuery.CountAsync(x => x.Quotes.Any(q => q.Status == CustomerQuoteStatus.Draft), ct),
            await baseQuery.CountAsync(x => x.Status == RfqStatus.Negotiating, ct),
            await baseQuery.CountAsync(x => x.Status == RfqStatus.Accepted, ct),
            await baseQuery.CountAsync(x => x.Status == RfqStatus.Expired || x.Quotes.SelectMany(q => q.Versions).Any(v => v.ValidUntil < now), ct));
        return new(data.Select(x =>
        {
            var latest = x.Versions.FirstOrDefault();
            var status = latest is not null && latest.ValidUntil < now && x.Status == RfqStatus.Quoted ? RfqStatus.Expired.ToString() : x.Status.ToString();
            return new AdminQuoteRowDto(x.Id, x.Number, x.Title, x.Company, x.Customer, status, x.ItemCount,
                x.QuoteDeadline, latest?.Total, x.Versions.Count, x.CreatedAt);
        }).ToList(), total, page, pageSize, summary);
    }

    public async Task<AdminQuoteDetailDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.AsNoTracking().Include(x => x.Items).Include(x => x.Quotes).ThenInclude(x => x.Versions)
            .ThenInclude(x => x.Items).Include(x => x.Negotiations).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("طلب عرض السعر غير موجود");
        var company = await db.Companies.AsNoTracking().FirstAsync(x => x.TenantId == rfq.TenantId, ct);
        var customer = await db.Users.AsNoTracking().FirstAsync(x => x.Id == rfq.UserId, ct);
        var temp = await db.RfqTemporaryProducts.AsNoTracking().Where(x => x.TenantId == rfq.TenantId && rfq.Items.Select(i => i.Id).Contains(x.RfqItemId))
            .ToDictionaryAsync(x => x.RfqItemId, ct);
        var requests = await db.SupplierQuoteRequests.AsNoTracking().Where(x => x.RfqId == id).OrderByDescending(x => x.SentAt).ToListAsync(ct);
        var supplierQuotes = await db.SupplierQuotes.AsNoTracking().Include(x => x.Items).Where(x => x.RfqId == id).OrderBy(x => x.Total).ToListAsync(ct);
        var supplierIds = requests.Select(x => x.SupplierId).Concat(supplierQuotes.Select(x => x.SupplierId)).Distinct().ToList();
        var suppliers = await db.Suppliers.AsNoTracking().OrderBy(x => x.NameAr).ToListAsync(ct);
        var supplierNames = suppliers.Where(x => supplierIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.NameAr);
        var actors = await db.Users.AsNoTracking().Where(x => rfq.Negotiations.Select(n => n.UserId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var assigned = rfq.AssignedStaffId is { } staffId
            ? await db.Users.AsNoTracking().Where(x => x.Id == staffId).Select(x => x.FullName).FirstOrDefaultAsync(ct) : null;
        var quote = rfq.Quotes.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var templates = await TemplatesAsync(ct);
        return new(rfq.Id, rfq.Number, rfq.Title, rfq.Description, EffectiveStatus(rfq).ToString(), company.LegalName,
            customer.FullName, customer.Phone, customer.Email, rfq.RequiredDate, rfq.QuoteDeadline, rfq.DeliveryGovernorate,
            rfq.AssignedStaffId, assigned, quote?.Number, quote?.Status.ToString(), rfq.ConvertedOrderId,
            rfq.Items.OrderBy(x => x.CreatedAt).Select(x => new AdminQuoteItemDto(x.Id, x.ProductId, x.DescriptionAr, x.SkuHint,
                x.Quantity, x.UnitName, x.Specifications, x.PreferredBrand, x.AllowAlternatives, x.Source.ToString(),
                x.ExtractionConfidence, x.IsReviewed, temp.ContainsKey(x.Id), temp.GetValueOrDefault(x.Id)?.EstimatedCost,
                temp.GetValueOrDefault(x.Id)?.DeliveryDays)).ToList(),
            requests.Select(x => new AdminSupplierRequestDto(x.Id, x.SupplierId, supplierNames.GetValueOrDefault(x.SupplierId, "مورد"), x.SentAt, x.Deadline, x.Status)).ToList(),
            supplierQuotes.Select(x => new AdminSupplierQuoteDto(x.Id, x.SupplierId, supplierNames.GetValueOrDefault(x.SupplierId, "مورد"),
                x.Number, x.Total, x.ValidUntil, x.Status, x.Items.Select(i => new AdminSupplierQuoteItemDto(i.RfqItemId, i.Quantity,
                    i.UnitPrice, i.LineTotal, i.AlternativeDescription)).ToList())).ToList(),
            quote?.Versions.OrderByDescending(x => x.VersionNumber).Select(Version).ToList() ?? [],
            rfq.Negotiations.OrderBy(x => x.CreatedAt).Select(x => new AdminNegotiationDto(x.Id,
                actors.GetValueOrDefault(x.UserId, x.IsStaff ? "فريق المبيعات" : customer.FullName), x.IsStaff, x.Type.ToString(),
                x.Message, x.ProposedTotal, x.CreatedAt)).ToList(),
            suppliers.Select(SupplierDto).ToList(), templates);
    }

    public async Task<AdminQuoteDetailDto> ReviewItemAsync(Guid id, Guid itemId, ReviewAdminRfqItemDto dto, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var item = rfq.Items.FirstOrDefault(x => x.Id == itemId) ?? throw ApiException.NotFound("الصنف غير موجود");
        if (dto.Quantity <= 0) throw ApiException.BadRequest("الكمية يجب أن تكون أكبر من صفر");
        item.DescriptionAr = Required(dto.Description, 300); item.Quantity = dto.Quantity; item.UnitName = Required(dto.UnitName, 50);
        item.Specifications = Clean(dto.Specifications, 1500); item.PreferredBrand = Clean(dto.PreferredBrand, 100);
        item.AllowAlternatives = dto.AllowAlternatives; item.IsReviewed = dto.IsReviewed;
        rfq.Status = rfq.Items.All(x => x.Id == itemId || x.IsReviewed) ? RfqStatus.UnderReview : RfqStatus.NeedsReview;
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> LinkProductAsync(Guid id, Guid itemId, Guid productId, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var item = rfq.Items.FirstOrDefault(x => x.Id == itemId) ?? throw ApiException.NotFound("الصنف غير موجود");
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId && x.Status == ProductStatus.Active, ct)
            ?? throw ApiException.NotFound("المنتج غير متاح");
        item.ProductId = product.Id; item.DescriptionAr = product.NameAr; item.SkuHint = product.Sku; item.IsReviewed = true;
        var oldTemp = await db.RfqTemporaryProducts.FirstOrDefaultAsync(x => x.RfqItemId == itemId, ct); if (oldTemp is not null) db.RfqTemporaryProducts.Remove(oldTemp);
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> CreateTemporaryAsync(Guid id, Guid itemId, CreateTemporaryRfqProductDto dto, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var item = rfq.Items.FirstOrDefault(x => x.Id == itemId) ?? throw ApiException.NotFound("الصنف غير موجود");
        if (dto.EstimatedCost < 0 || dto.DeliveryDays <= 0) throw ApiException.BadRequest("بيانات المنتج المؤقت غير صالحة");
        var temp = await db.RfqTemporaryProducts.FirstOrDefaultAsync(x => x.RfqItemId == itemId, ct);
        if (temp is null) { temp = new RfqTemporaryProduct { TenantId = rfq.TenantId, RfqItemId = itemId }; db.RfqTemporaryProducts.Add(temp); }
        temp.NameAr = Required(dto.Name, 300); temp.Specifications = Clean(dto.Specifications, 1500);
        temp.EstimatedCost = dto.EstimatedCost; temp.DeliveryDays = dto.DeliveryDays;
        item.ProductId = null; item.SkuHint = $"TMP-{item.Id.ToString("N")[..8].ToUpperInvariant()}"; item.DescriptionAr = temp.NameAr;
        item.Specifications = temp.Specifications; item.IsReviewed = true;
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> RequestSupplierAsync(Guid id, RequestSupplierPriceDto dto, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); if (dto.Deadline <= DateTime.UtcNow) throw ApiException.BadRequest("موعد رد المورد يجب أن يكون في المستقبل");
        Supplier? supplier = null;
        if (dto.SupplierId is { } supplierId) supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == supplierId && x.IsActive, ct);
        if (supplier is null)
        {
            supplier = new Supplier { Code = $"SUP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}", NameAr = Required(dto.SupplierName, 200),
                ContactName = Clean(dto.ContactName, 150), Phone = Clean(dto.Phone, 30), Email = Clean(dto.Email, 200) };
            db.Suppliers.Add(supplier);
        }
        var existing = await db.SupplierQuoteRequests.FirstOrDefaultAsync(x => x.RfqId == id && x.SupplierId == supplier.Id && x.Status == "Sent", ct);
        if (existing is null) db.SupplierQuoteRequests.Add(new SupplierQuoteRequest { TenantId = rfq.TenantId, RfqId = id,
            SupplierId = supplier.Id, SentAt = DateTime.UtcNow, Deadline = dto.Deadline, Status = "Sent" });
        else existing.Deadline = dto.Deadline;
        rfq.Status = RfqStatus.UnderReview; await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> RecordSupplierPriceAsync(Guid id, RecordSupplierPriceDto dto, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var supplier = await db.Suppliers.FirstOrDefaultAsync(x => x.Id == dto.SupplierId && x.IsActive, ct)
            ?? throw ApiException.NotFound("المورد غير موجود");
        if (dto.ValidUntil <= DateTime.UtcNow || dto.Items.Count == 0 || dto.Items.Any(x => x.UnitPrice < 0)) throw ApiException.BadRequest("عرض المورد غير صالح");
        var ids = rfq.Items.Select(x => x.Id).ToHashSet(); if (dto.Items.Any(x => !ids.Contains(x.RfqItemId))) throw ApiException.BadRequest("صنف غير موجود في الطلب");
        var quote = new SupplierQuote { TenantId = rfq.TenantId, RfqId = id, SupplierId = supplier.Id,
            Number = Clean(dto.Number, 80) ?? $"SQ-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}", ValidUntil = dto.ValidUntil };
        foreach (var line in dto.Items)
        {
            var source = rfq.Items.First(x => x.Id == line.RfqItemId); quote.Items.Add(new SupplierQuoteItem { TenantId = rfq.TenantId,
                RfqItemId = source.Id, Quantity = source.Quantity, UnitPrice = line.UnitPrice, LineTotal = decimal.Round(source.Quantity * line.UnitPrice, 2),
                AlternativeDescription = Clean(line.AlternativeDescription, 500) });
        }
        quote.Total = quote.Items.Sum(x => x.LineTotal); db.SupplierQuotes.Add(quote);
        var request = await db.SupplierQuoteRequests.FirstOrDefaultAsync(x => x.RfqId == id && x.SupplierId == supplier.Id && x.Status == "Sent", ct);
        if (request is not null) request.Status = "Received"; await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> SaveQuoteAsync(Guid id, SaveCustomerQuoteDto dto, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct);
        if (dto.Items.Count == 0 || dto.ValidDays is < 1 or > 180 || dto.DeliveryDays <= 0 || dto.Shipping < 0 || dto.Items.Any(x => x.UnitPrice < 0 || x.CostPrice < 0 || x.DeliveryDays <= 0))
            throw ApiException.BadRequest("بيانات عرض السعر غير مكتملة");
        var itemIds = rfq.Items.Select(x => x.Id).ToHashSet();
        if (dto.Items.Any(x => !itemIds.Contains(x.RfqItemId)) || itemIds.Any(x => !dto.Items.Any(i => i.RfqItemId == x)))
            throw ApiException.BadRequest("يجب تسعير جميع الأصناف المطلوبة");
        var quote = await db.CustomerQuotes.Include(x => x.Versions).FirstOrDefaultAsync(x => x.RfqId == id, ct);
        if (quote is null) { quote = new CustomerQuote { TenantId = rfq.TenantId, RfqId = id,
            Number = $"QT-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}" }; db.CustomerQuotes.Add(quote); }
        var discountType = NormalizeDiscount(dto.DiscountType); var version = new CustomerQuoteVersion { TenantId = rfq.TenantId,
            QuoteId = quote.Id, VersionNumber = quote.CurrentVersion + 1, Shipping = dto.Shipping, DiscountType = discountType,
            DiscountValue = dto.DiscountValue, ValidUntil = DateTime.UtcNow.AddDays(dto.ValidDays), DeliveryDays = dto.DeliveryDays,
            Terms = Clean(dto.Terms, 3000), ChangeSummary = Clean(dto.ChangeSummary, 1000), SentAt = dto.SendNow ? DateTime.UtcNow : DateTime.MinValue };
        foreach (var input in dto.Items)
        {
            var source = rfq.Items.First(x => x.Id == input.RfqItemId); var description = Clean(input.Description, 300) ?? source.DescriptionAr;
            version.Items.Add(new CustomerQuoteItem { TenantId = rfq.TenantId, RfqItemId = source.Id, ProductId = input.ProductId ?? source.ProductId,
                DescriptionAr = description, Quantity = source.Quantity, UnitName = source.UnitName, CostPrice = input.CostPrice,
                UnitPrice = input.UnitPrice, LineTotal = decimal.Round(source.Quantity * input.UnitPrice, 2), DeliveryDays = input.DeliveryDays,
                IsAlternative = input.IsAlternative, AlternativeReason = Clean(input.AlternativeReason, 500) });
        }
        version.Subtotal = version.Items.Sum(x => x.LineTotal);
        version.DiscountAmount = discountType == "Percent" ? decimal.Round(version.Subtotal * Math.Clamp(dto.DiscountValue, 0, 100) / 100, 2)
            : discountType == "Fixed" ? Math.Clamp(dto.DiscountValue, 0, version.Subtotal) : 0;
        var taxable = version.Subtotal - version.DiscountAmount; version.Tax = decimal.Round(taxable * .14m, 2); version.Total = taxable + version.Tax + version.Shipping;
        db.CustomerQuoteVersions.Add(version); quote.CurrentVersion = version.VersionNumber;
        quote.Status = dto.SendNow ? CustomerQuoteStatus.Sent : CustomerQuoteStatus.Draft;
        rfq.Status = dto.SendNow ? RfqStatus.Quoted : RfqStatus.UnderReview;
        if (dto.SendNow) AddQuoteNotification(rfq, quote, version);
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> SendAsync(Guid id, Guid versionId, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var quote = await db.CustomerQuotes.Include(x => x.Versions).FirstOrDefaultAsync(x => x.RfqId == id, ct)
            ?? throw ApiException.NotFound("عرض السعر غير موجود");
        var version = quote.Versions.FirstOrDefault(x => x.Id == versionId) ?? throw ApiException.NotFound("نسخة العرض غير موجودة");
        if (version.ValidUntil <= DateTime.UtcNow) throw ApiException.Conflict("انتهت صلاحية هذه النسخة");
        version.SentAt = DateTime.UtcNow; quote.Status = CustomerQuoteStatus.Sent; rfq.Status = RfqStatus.Quoted;
        AddQuoteNotification(rfq, quote, version); await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> NegotiateAsync(Guid staffId, Guid id, StaffNegotiationDto dto, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); if (!Enum.TryParse<NegotiationMessageType>(dto.Type, true, out var type)) type = NegotiationMessageType.Message;
        db.QuoteNegotiations.Add(new QuoteNegotiation { TenantId = rfq.TenantId, RfqId = id, UserId = staffId, IsStaff = true,
            Type = type, Message = Required(dto.Message, 2000), ProposedTotal = dto.ProposedTotal });
        rfq.Status = RfqStatus.Negotiating; var quote = await db.CustomerQuotes.FirstOrDefaultAsync(x => x.RfqId == id, ct);
        if (quote is not null) quote.Status = CustomerQuoteStatus.RevisionRequested;
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<AdminQuoteDetailDto> AcceptAsync(Guid id, Guid versionId, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var quote = await db.CustomerQuotes.Include(x => x.Versions).FirstOrDefaultAsync(x => x.RfqId == id, ct)
            ?? throw ApiException.NotFound("عرض السعر غير موجود");
        var version = quote.Versions.FirstOrDefault(x => x.Id == versionId) ?? throw ApiException.NotFound("نسخة العرض غير موجودة");
        if (version.ValidUntil <= DateTime.UtcNow) throw ApiException.Conflict("انتهت صلاحية العرض");
        quote.Status = CustomerQuoteStatus.Accepted; quote.AcceptedVersionId = version.Id; rfq.AcceptedQuoteId = quote.Id; rfq.Status = RfqStatus.Accepted;
        await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public async Task<ConvertedRfqOrderDto> ConvertAsync(Guid id, ConvertRfqDto dto, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("طلب العرض غير موجود");
        return await rfqService.ConvertAsync(rfq.UserId, id, dto, ct);
    }

    public async Task<AdminQuoteDetailDto> AssignAsync(Guid id, Guid staffId, CancellationToken ct = default)
    {
        var rfq = await GetAsync(id, ct); var staff = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == staffId && x.IsPlatformStaff && x.IsActive, ct)
            ?? throw ApiException.BadRequest("الموظف غير صالح"); rfq.AssignedStaffId = staff.Id; await db.SaveChangesAsync(ct); return await DetailAsync(id, ct);
    }

    public Task<List<AdminProductOptionDto>> ProductsAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking().Where(x => x.Status == ProductStatus.Active);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Sku.Contains(term) || x.NameAr.Contains(term)); }
        return query.OrderBy(x => x.NameAr).Take(80).Select(x => new AdminProductOptionDto(x.Id, x.Sku, x.NameAr, x.BasePrice, x.StockQty, x.DeliveryEstimateDays)).ToListAsync(ct);
    }

    public async Task<RfqConversionOptionsDto> ConversionOptionsAsync(Guid id, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("طلب العرض غير موجود");
        var branchRows = await db.CompanyBranches.AsNoTracking().Where(x => x.TenantId == rfq.TenantId).OrderByDescending(x => x.IsMain)
            .Select(x => new { x.Id, x.Name, x.Governorate, x.City, x.AddressLine }).ToListAsync(ct);
        var branches = branchRows.Select(x => new RfqBranchDto(x.Id, x.Name,
            string.Join(" - ", new[] { x.Governorate, x.City, x.AddressLine }.Where(v => !string.IsNullOrWhiteSpace(v))))).ToList();
        var centers = await db.CostCenters.AsNoTracking().Where(x => x.TenantId == rfq.TenantId && x.IsActive).OrderBy(x => x.Code)
            .Select(x => new RfqCostCenterDto(x.Id, x.Code, x.NameAr, x.BudgetAmount - x.UsedAmount - x.ReservedAmount)).ToListAsync(ct);
        var receivers = await db.Users.AsNoTracking().Where(x => x.TenantId == rfq.TenantId && x.IsActive).OrderBy(x => x.FullName)
            .Select(x => new RfqReceiverDto(x.Id, x.FullName, x.Phone)).ToListAsync(ct);
        return new(branches, centers, receivers);
    }

    public async Task<byte[]> PdfAsync(Guid id, Guid versionId, CancellationToken ct = default)
    {
        var rfq = await db.Rfqs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("طلب العرض غير موجود");
        return await rfqService.QuotePdfAsync(rfq.UserId, id, versionId, ct);
    }

    public Task<List<AdminQuoteTemplateDto>> TemplatesAsync(CancellationToken ct = default) => db.QuoteTemplates.AsNoTracking().OrderByDescending(x => x.IsActive)
        .ThenBy(x => x.NameAr).Select(x => new AdminQuoteTemplateDto(x.Id, x.NameAr, x.Description, x.ValidDays, x.DeliveryDays,
            x.Terms, x.DiscountType, x.DiscountValue, x.IsActive)).ToListAsync(ct);

    public async Task<AdminQuoteTemplateDto> SaveTemplateAsync(Guid? id, SaveQuoteTemplateDto dto, CancellationToken ct = default)
    {
        if (dto.ValidDays is < 1 or > 180 || dto.DeliveryDays <= 0) throw ApiException.BadRequest("بيانات القالب غير صالحة");
        var template = id is null ? new QuoteTemplate() : await db.QuoteTemplates.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("القالب غير موجود");
        if (id is null) db.QuoteTemplates.Add(template); template.NameAr = Required(dto.Name, 150); template.Description = Clean(dto.Description, 500);
        template.ValidDays = dto.ValidDays; template.DeliveryDays = dto.DeliveryDays; template.Terms = Clean(dto.Terms, 3000);
        template.DiscountType = NormalizeDiscount(dto.DiscountType); template.DiscountValue = dto.DiscountValue; template.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct); return new(template.Id, template.NameAr, template.Description, template.ValidDays, template.DeliveryDays,
            template.Terms, template.DiscountType, template.DiscountValue, template.IsActive);
    }

    private async Task<RfqEntity> GetAsync(Guid id, CancellationToken ct) => await db.Rfqs.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw ApiException.NotFound("طلب عرض السعر غير موجود");
    private static string NormalizeDiscount(string? value) => value?.Trim().ToLowerInvariant() switch { "percent" => "Percent", "fixed" => "Fixed", _ => "None" };
    private static RfqStatus EffectiveStatus(RfqEntity rfq) => rfq.Status == RfqStatus.Quoted && rfq.Quotes.SelectMany(x => x.Versions).Any() &&
        rfq.Quotes.SelectMany(x => x.Versions).Max(x => x.ValidUntil) < DateTime.UtcNow ? RfqStatus.Expired : rfq.Status;
    private static AdminSupplierDto SupplierDto(Supplier x) => new(x.Id, x.Code, x.NameAr, x.ContactName, x.Phone, x.Email, x.TypicalLeadDays, x.Rating, x.IsActive);
    private static AdminCustomerQuoteVersionDto Version(CustomerQuoteVersion x) => new(x.Id, x.VersionNumber, x.Subtotal, x.Tax, x.Shipping,
        x.DiscountType, x.DiscountValue, x.DiscountAmount, x.Total, x.ValidUntil, x.DeliveryDays, x.Terms, x.ChangeSummary, x.SentAt,
        x.ValidUntil < DateTime.UtcNow, x.Items.Select(i => new AdminCustomerQuoteItemDto(i.Id, i.RfqItemId, i.ProductId, i.DescriptionAr,
            i.Quantity, i.UnitName, i.CostPrice, i.UnitPrice, i.LineTotal, i.DeliveryDays, i.IsAlternative, i.AlternativeReason)).ToList());
    private void AddQuoteNotification(RfqEntity rfq, CustomerQuote quote, CustomerQuoteVersion version) => db.Notifications.Add(new AppNotification
    {
        TenantId = rfq.TenantId, UserId = rfq.UserId, Type = "rfq.quoted", Title = "وصل عرض السعر",
        Body = $"العرض {quote.Number} بقيمة {version.Total:0.##} ج.م", EntityType = nameof(RfqEntity), EntityId = rfq.Id
    });
    private static string Required(string? value, int max) => string.IsNullOrWhiteSpace(value) ? throw ApiException.BadRequest("البيان المطلوب غير مكتمل") : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
