using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminContracts;

public sealed class AdminContractService(AppDbContext db)
{
    public async Task<ContractDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var contracts = await db.CompanyContracts.AsNoTracking().Include(x => x.Products).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var companies = await db.Companies.AsNoTracking().Include(x => x.Tenant).ToListAsync(ct);
        var products = await db.Products.AsNoTracking().Where(x => x.Status == ProductStatus.Active).OrderBy(x => x.NameAr).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().Where(x => x.Status != OrderStatus.Cancelled).ToListAsync(ct);
        var staff = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var productMap = products.ToDictionary(x => x.Id);
        var companyMap = companies.ToDictionary(x => x.Id);
        var rows = contracts.Where(x => companyMap.ContainsKey(x.CompanyId)).Select(x => MapRow(x, companyMap[x.CompanyId], orders.Where(o => o.TenantId == x.TenantId), productMap, staff, now)).ToList();
        var active = rows.Where(x => x.Status is "Active" or "Expiring").ToList();
        var savings = active.Count == 0 ? 0 : Math.Round(active.Average(x => x.AverageSavingPercent), 1);
        var companyOptions = companies.Select(c => new ContractCompanyOptionDto(c.Id, c.TenantId, c.LegalName, c.Tenant.Status.ToString(), orders.Where(x => x.TenantId == c.TenantId && x.CreatedAt >= now.AddYears(-1)).Sum(x => x.Total), contracts.Count(x => x.CompanyId == c.Id), c.CreditLimit)).OrderBy(x => x.Name).ToList();
        return new(new(active.Count, active.Sum(x => x.AnnualValue), rows.Count(x => x.Status == "Expiring"), savings), rows, companyOptions, products.Select(x => new ContractProductOptionDto(x.Id, x.Sku, x.NameAr, x.BasePrice, x.CostPrice)).ToList());
    }

    public async Task<ContractDetailDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        var contract = await db.CompanyContracts.AsNoTracking().Include(x => x.Products).Include(x => x.QuantityTiers).Include(x => x.Attachments).Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("العقد غير موجود");
        var company = await db.Companies.AsNoTracking().Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == contract.CompanyId, ct) ?? throw ApiException.NotFound("الشركة غير موجودة");
        var products = await db.Products.AsNoTracking().Where(x => contract.Products.Select(p => p.ProductId).Contains(x.Id)).ToListAsync(ct);
        var productMap = products.ToDictionary(x => x.Id);
        var orders = await db.Orders.AsNoTracking().Where(x => x.TenantId == contract.TenantId && x.Status != OrderStatus.Cancelled && x.CreatedAt >= contract.StartsAt && x.CreatedAt <= contract.EndsAt).ToListAsync(ct);
        var allUsers = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff || x.TenantId == contract.TenantId).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var revisions = await db.ContractPriceRevisions.AsNoTracking().Include(x => x.Items).Where(x => x.ContractId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var row = MapRow(contract, company, orders, productMap, allUsers, DateTime.UtcNow);
        var productDtos = contract.Products.Where(x => productMap.ContainsKey(x.ProductId)).Select(x => ProductDto(x, productMap[x.ProductId])).ToList();
        var totalPurchases = orders.Sum(x => x.Total); var avgMargin = productDtos.Count == 0 ? 0 : Math.Round(productDtos.Average(x => x.MarginPercent), 1); var saving = productDtos.Sum(x => x.CustomerSaving * x.EstimatedAnnualQuantity);
        return new(row, contract.PricingMode.ToString(), contract.MarketDiscountPercent, contract.PaymentTermsDays, contract.CreditLimit, contract.AutoRenew, contract.RenewalRequiresApproval, contract.ExpiryAlertDays, contract.EarlyPaymentDiscountPercent, contract.EarlyPaymentDays, contract.LatePaymentPenaltyPercent, contract.PaymentMethod, contract.DeliveryPriority, contract.DeliveryHours, contract.DeliveryLatePenaltyPercent, contract.FreeShipping, contract.CreditReviewMonths, contract.TermsSummary, contract.ActivatedAt, contract.CustomerNotifiedAt,
            productDtos,
            contract.QuantityTiers.OrderBy(x => x.MinQuantity).Select(x => new ContractTierDto(x.Id, x.MinQuantity, x.MaxQuantity, x.AdditionalDiscountPercent)).ToList(),
            contract.Attachments.OrderByDescending(x => x.CreatedAt).Select(x => new ContractAttachmentDto(x.Id, x.Type.ToString(), x.FileName, x.StoragePath, x.ContentType, x.SizeBytes, x.CreatedAt)).ToList(),
            contract.Approvals.OrderBy(x => x.Sequence).Select(x => new ContractApprovalDto(x.Id, x.Sequence, x.RoleCode, x.LabelAr, x.AssignedUserId is Guid assigned ? Name(allUsers, assigned) : null, x.Status.ToString(), x.DecidedByUserId is Guid decided ? Name(allUsers, decided) : null, x.DecidedAt, x.DecisionNote)).ToList(),
            revisions.Select(x => new ContractRevisionDto(x.Id, x.EffectiveAt, x.CustomerApprovedAt, x.Reason, x.Status.ToString(), x.Items.Select(i => new ContractRevisionItemDto(i.ProductId, productMap.TryGetValue(i.ProductId, out var p) ? p.NameAr : "منتج", i.OldPrice, i.NewPrice, i.Reason)).ToList())).ToList(),
            new(saving, avgMargin, avgMargin >= 10 ? "Healthy" : avgMargin >= 0 ? "Review" : "Loss", row.ConsumptionPercent, orders.Count, totalPurchases));
    }

    public async Task<Guid> CreateAsync(Guid actor, SaveContractDto dto, CancellationToken ct = default)
    {
        var company = await Company(dto.CompanyId, ct); var products = await Validate(dto, ct);
        var next = await db.CompanyContracts.CountAsync(x => x.CreatedAt.Year == DateTime.UtcNow.Year, ct) + 1;
        var contract = new CompanyContract { TenantId = company.TenantId, CompanyId = company.Id, Number = $"CT-{DateTime.UtcNow.Year}-{next:000}", CreatedBy = actor };
        Apply(contract, dto, products); contract.Status = CompanyContractStatus.PendingApproval; db.CompanyContracts.Add(contract); BuildApprovals(contract, dto);
        Audit(actor, company.TenantId, "contract.create", contract.Id, contract.Number); await db.SaveChangesAsync(ct); return contract.Id;
    }

    public async Task UpdateDraftAsync(Guid actor, Guid id, SaveContractDto dto, CancellationToken ct = default)
    {
        var contract = await db.CompanyContracts.Include(x => x.Products).Include(x => x.QuantityTiers).Include(x => x.Approvals).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("العقد غير موجود");
        if (contract.Status is not (CompanyContractStatus.Draft or CompanyContractStatus.PendingApproval)) throw ApiException.Conflict("لا يمكن تعديل بيانات عقد نشط من شاشة المسودة");
        var company = await Company(dto.CompanyId, ct); if (company.TenantId != contract.TenantId) throw ApiException.Conflict("لا يمكن تغيير شركة العقد بعد إنشائه"); var products = await Validate(dto, ct);
        db.CompanyContractProducts.RemoveRange(contract.Products); db.CompanyContractQuantityTiers.RemoveRange(contract.QuantityTiers); db.CompanyContractApprovals.RemoveRange(contract.Approvals); contract.Products = []; contract.QuantityTiers = []; contract.Approvals = [];
        Apply(contract, dto, products); contract.Status = CompanyContractStatus.PendingApproval; contract.UpdatedBy = actor; BuildApprovals(contract, dto); Audit(actor, contract.TenantId, "contract.update", id, contract.Number); await db.SaveChangesAsync(ct);
    }

    public async Task AddAttachmentAsync(Guid actor, Guid id, AddContractAttachmentDto dto, CancellationToken ct = default)
    {
        var contract = await Contract(id, ct); if (string.IsNullOrWhiteSpace(dto.FileName) || string.IsNullOrWhiteSpace(dto.StoragePath) || dto.SizeBytes <= 0 || dto.SizeBytes > 20 * 1024 * 1024) throw ApiException.BadRequest("بيانات المرفق أو حجمه غير صالح");
        db.CompanyContractAttachments.Add(new CompanyContractAttachment { TenantId = contract.TenantId, ContractId = id, Type = Parse<ContractAttachmentType>(dto.Type, "نوع المرفق غير صحيح"), FileName = dto.FileName.Trim(), StoragePath = dto.StoragePath.Trim(), ContentType = string.IsNullOrWhiteSpace(dto.ContentType) ? "application/pdf" : dto.ContentType.Trim(), SizeBytes = dto.SizeBytes, CreatedBy = actor }); await db.SaveChangesAsync(ct);
    }

    public async Task DecideApprovalAsync(Guid actor, Guid contractId, Guid approvalId, DecideContractApprovalDto dto, CancellationToken ct = default)
    {
        var contract = await Contract(contractId, ct); if (contract.Status != CompanyContractStatus.PendingApproval) throw ApiException.Conflict("العقد ليس في مرحلة الموافقات");
        var approval = await db.CompanyContractApprovals.FirstOrDefaultAsync(x => x.Id == approvalId && x.ContractId == contractId, ct) ?? throw ApiException.NotFound("الموافقة غير موجودة");
        if (approval.Status != ContractApprovalStatus.Pending) throw ApiException.Conflict("تم اتخاذ قرار في هذه الموافقة بالفعل");
        if (await db.CompanyContractApprovals.AnyAsync(x => x.ContractId == contractId && x.Sequence < approval.Sequence && x.Status != ContractApprovalStatus.Approved, ct)) throw ApiException.Conflict("يجب إكمال الموافقات السابقة أولًا");
        approval.Status = Parse<ContractApprovalStatus>(dto.Decision, "قرار الموافقة غير صحيح"); if (approval.Status == ContractApprovalStatus.Pending) throw ApiException.BadRequest("القرار يجب أن يكون اعتمادًا أو رفضًا"); approval.DecidedByUserId = actor; approval.DecidedAt = DateTime.UtcNow; approval.DecisionNote = Empty(dto.Note);
        if (approval.Status == ContractApprovalStatus.Rejected) contract.Status = CompanyContractStatus.Draft; Audit(actor, contract.TenantId, "contract.approval", contractId, $"{approval.Status}: {dto.Note}"); await db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid actor, Guid id, ActivateContractDto dto, CancellationToken ct = default)
    {
        var contract = await db.CompanyContracts.Include(x => x.Products).Include(x => x.Approvals).Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("العقد غير موجود");
        if (contract.Status != CompanyContractStatus.PendingApproval || contract.Approvals.Any(x => x.Status != ContractApprovalStatus.Approved)) throw ApiException.Conflict("لا يمكن التفعيل قبل اكتمال الموافقات");
        if (!contract.Attachments.Any(x => x.Type == ContractAttachmentType.SignedContract)) throw ApiException.Conflict("يجب إرفاق نسخة العقد الموقعة قبل التفعيل"); if (contract.Products.Count == 0) throw ApiException.Conflict("لا يمكن تفعيل عقد بدون منتجات");
        contract.Status = CompanyContractStatus.Active; contract.ActivatedAt = DateTime.UtcNow; contract.ActivatedByUserId = actor;
        if (dto.ApplyPricesImmediately) await ApplyPrices(contract, actor, ct);
        if (dto.NotifyCustomer) { var users = await db.Users.Where(x => x.TenantId == contract.TenantId && x.IsActive).ToListAsync(ct); db.Notifications.AddRange(users.Select(x => new AppNotification { TenantId = contract.TenantId, UserId = x.Id, Type = "contract.activated", Title = "تم تفعيل العقد", Body = $"تم تفعيل العقد {contract.Number} وتطبيق أسعاره الخاصة.", EntityType = nameof(CompanyContract), EntityId = contract.Id })); contract.CustomerNotifiedAt = DateTime.UtcNow; }
        var company = await db.Companies.FirstAsync(x => x.Id == contract.CompanyId, ct); if (contract.CreditLimit > company.CreditLimit) company.CreditLimit = contract.CreditLimit; Audit(actor, contract.TenantId, "contract.activate", id, contract.Number); await db.SaveChangesAsync(ct);
    }

    public async Task RenewAsync(Guid actor, Guid id, RenewContractDto dto, CancellationToken ct = default)
    {
        var contract = await db.CompanyContracts.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("العقد غير موجود"); if (contract.Status is not (CompanyContractStatus.Active or CompanyContractStatus.Expiring or CompanyContractStatus.Expired)) throw ApiException.Conflict("حالة العقد لا تسمح بالتجديد"); if (dto.Months is < 1 or > 60 || dto.PriceAdjustmentPercent is < -50 or > 100) throw ApiException.BadRequest("شروط التجديد غير صالحة");
        var previousEnd = contract.EndsAt; contract.EndsAt = contract.EndsAt.AddMonths(dto.Months); contract.Status = CompanyContractStatus.Active; if (dto.CreditLimit is decimal limit) contract.CreditLimit = Math.Max(0, limit); foreach (var p in contract.Products) p.ContractPrice = Math.Round(p.ContractPrice * (1 + dto.PriceAdjustmentPercent / 100m), 2);
        db.ContractRenewalRequests.Add(new ContractRenewalRequest { TenantId = contract.TenantId, ContractId = id, RequestedByUserId = actor, RequestedMonths = dto.Months, Note = Empty(dto.Note) ?? $"تجديد من {previousEnd:yyyy-MM-dd}", Status = ContractRenewalStatus.Approved, DecidedAt = DateTime.UtcNow, DecidedByUserId = actor, DecisionNote = $"تعديل أسعار {dto.PriceAdjustmentPercent}%" }); await ApplyPrices(contract, actor, ct); Audit(actor, contract.TenantId, "contract.renew", id, $"{dto.Months} months; {dto.PriceAdjustmentPercent}%"); await db.SaveChangesAsync(ct);
    }

    public async Task CreatePriceRevisionAsync(Guid actor, Guid id, CreateContractPriceRevisionDto dto, CancellationToken ct = default)
    {
        var contract = await db.CompanyContracts.Include(x => x.Products).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("العقد غير موجود"); if (contract.Status is not (CompanyContractStatus.Active or CompanyContractStatus.Expiring)) throw ApiException.Conflict("يمكن تعديل أسعار العقود النشطة فقط"); if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Items.Count == 0 || dto.Items.Select(x => x.ProductId).Distinct().Count() != dto.Items.Count || dto.Items.Any(x => x.NewPrice <= 0)) throw ApiException.BadRequest("بيانات مراجعة الأسعار غير صالحة");
        var current = contract.Products.ToDictionary(x => x.ProductId); if (dto.Items.Any(x => !current.ContainsKey(x.ProductId))) throw ApiException.BadRequest("أحد المنتجات غير مشمول بالعقد");
        var applyNow = dto.CustomerApproved && dto.EffectiveAt <= DateTime.UtcNow;
        var revision = new ContractPriceRevision { TenantId = contract.TenantId, ContractId = id, EffectiveAt = dto.EffectiveAt, CustomerApprovedAt = dto.CustomerApproved ? DateTime.UtcNow : null, Reason = dto.Reason.Trim(), Status = !dto.CustomerApproved ? ContractPriceRevisionStatus.PendingCustomerApproval : applyNow ? ContractPriceRevisionStatus.Applied : ContractPriceRevisionStatus.Scheduled, CreatedBy = actor, Items = dto.Items.Select(x => new ContractPriceRevisionItem { TenantId = contract.TenantId, ProductId = x.ProductId, OldPrice = current[x.ProductId].ContractPrice, NewPrice = x.NewPrice, Reason = Empty(x.Reason) }).ToList() };
        db.ContractPriceRevisions.Add(revision); if (applyNow) { foreach (var item in dto.Items) current[item.ProductId].ContractPrice = item.NewPrice; await ApplyPrices(contract, actor, ct); } Audit(actor, contract.TenantId, "contract.price-revision", id, revision.Status.ToString()); await db.SaveChangesAsync(ct);
    }

    /// <summary>Applies customer-approved price revisions once their effective time arrives.</summary>
    public async Task<int> ProcessDuePriceRevisionsAsync(CancellationToken ct = default)
    {
        var due = await db.ContractPriceRevisions
            .Include(x => x.Items)
            .Include(x => x.Contract).ThenInclude(x => x.Products)
            .Where(x => x.Status == ContractPriceRevisionStatus.Scheduled && x.EffectiveAt <= DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var revision in due)
        {
            var prices = revision.Contract.Products.ToDictionary(x => x.ProductId);
            foreach (var item in revision.Items.Where(x => prices.ContainsKey(x.ProductId)))
                prices[item.ProductId].ContractPrice = item.NewPrice;

            var actor = revision.CreatedBy ?? Guid.Empty;
            await ApplyPrices(revision.Contract, actor, ct);
            revision.Status = ContractPriceRevisionStatus.Applied;
            Audit(actor, revision.TenantId, "contract.price-revision.apply", revision.ContractId, revision.Id.ToString());
        }

        if (due.Count > 0) await db.SaveChangesAsync(ct);
        return due.Count;
    }

    private async Task<Company> Company(Guid id, CancellationToken ct) => await db.Companies.Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == id && x.Tenant.Status != TenantStatus.Suspended && x.Tenant.Status != TenantStatus.Rejected, ct) ?? throw ApiException.NotFound("الشركة غير موجودة أو غير متاحة للتعاقد");
    private async Task<CompanyContract> Contract(Guid id, CancellationToken ct) => await db.CompanyContracts.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("العقد غير موجود");
    private async Task<Dictionary<Guid, Product>> Validate(SaveContractDto d, CancellationToken ct)
    {
        if (d.EndsAt.Date <= d.StartsAt.Date || d.Products.Count == 0 || d.Products.Select(x => x.ProductId).Distinct().Count() != d.Products.Count || d.Products.Any(x => x.EstimatedAnnualQuantity <= 0 || x.ContractPrice <= 0)) throw ApiException.BadRequest("مدة العقد أو المنتجات غير صالحة");
        if (d.PaymentTermsDays is < 0 or > 365 || d.CreditLimit < 0 || d.MarketDiscountPercent is < 0 or >= 100 || d.DeliveryHours <= 0 || d.ExpiryAlertDays is < 1 or > 365) throw ApiException.BadRequest("شروط العقد غير صالحة");
        var tiers = d.QuantityTiers.OrderBy(x => x.MinQuantity).ToList(); if (tiers.Any(x => x.MinQuantity < 1 || x.MaxQuantity < x.MinQuantity || x.AdditionalDiscountPercent is < 0 or >= 100) || tiers.Zip(tiers.Skip(1), (a, b) => a.MaxQuantity is null || a.MaxQuantity >= b.MinQuantity).Any(x => x)) throw ApiException.BadRequest("شرائح الكميات متداخلة أو غير صالحة");
        var ids = d.Products.Select(x => x.ProductId).ToList(); var products = await db.Products.Where(x => ids.Contains(x.Id) && x.Status == ProductStatus.Active).ToListAsync(ct); if (products.Count != ids.Count) throw ApiException.BadRequest("أحد منتجات العقد غير موجود أو غير نشط"); return products.ToDictionary(x => x.Id);
    }
    private static void Apply(CompanyContract c, SaveContractDto d, IReadOnlyDictionary<Guid, Product> products)
    {
        c.CompanyId = d.CompanyId; c.StartsAt = d.StartsAt; c.EndsAt = d.EndsAt; c.Type = Parse<CompanyContractType>(d.Type, "نوع العقد غير صحيح"); c.PricingMode = Parse<ContractPricingMode>(d.PricingMode, "آلية التسعير غير صحيحة"); c.MarketDiscountPercent = d.MarketDiscountPercent; c.AutoRenew = d.AutoRenew; c.RenewalRequiresApproval = d.RenewalRequiresApproval; c.ExpiryAlertDays = d.ExpiryAlertDays; c.PaymentTermsDays = d.PaymentTermsDays; c.CreditLimit = d.CreditLimit; c.EarlyPaymentDiscountPercent = d.EarlyPaymentDiscountPercent; c.EarlyPaymentDays = d.EarlyPaymentDays; c.LatePaymentPenaltyPercent = d.LatePaymentPenaltyPercent; c.PaymentMethod = d.PaymentMethod.Trim(); c.DeliveryPriority = d.DeliveryPriority.Trim(); c.DeliveryHours = d.DeliveryHours; c.DeliveryLatePenaltyPercent = d.DeliveryLatePenaltyPercent; c.FreeShipping = d.FreeShipping; c.CreditReviewMonths = d.CreditReviewMonths; c.TermsSummary = Empty(d.TermsSummary);
        c.Products = d.Products.Select(x => new CompanyContractProduct { TenantId = c.TenantId, ProductId = x.ProductId, ContractPrice = c.PricingMode == ContractPricingMode.MarketDiscount ? Math.Round(products[x.ProductId].BasePrice * (1 - d.MarketDiscountPercent / 100m), 2) : x.ContractPrice, EstimatedAnnualQuantity = x.EstimatedAnnualQuantity }).ToList(); c.QuantityTiers = d.QuantityTiers.Select(x => new CompanyContractQuantityTier { TenantId = c.TenantId, MinQuantity = x.MinQuantity, MaxQuantity = x.MaxQuantity, AdditionalDiscountPercent = x.AdditionalDiscountPercent }).ToList(); c.AnnualValue = d.AnnualValue > 0 ? d.AnnualValue : c.Products.Sum(x => x.ContractPrice * x.EstimatedAnnualQuantity);
    }
    private void BuildApprovals(CompanyContract c, SaveContractDto d)
    {
        c.Approvals.Add(new CompanyContractApproval { TenantId = c.TenantId, Sequence = 1, RoleCode = "sales_manager", LabelAr = "موافقة مدير المبيعات" });
        if (d.CreditLimit > 50_000) c.Approvals.Add(new CompanyContractApproval { TenantId = c.TenantId, Sequence = 2, RoleCode = "finance_manager", LabelAr = "موافقة المدير المالي" });
    }
    private async Task ApplyPrices(CompanyContract c, Guid actor, CancellationToken ct)
    {
        var productIds = c.Products.Select(x => x.ProductId).ToList(); var old = await db.CompanyProductPrices.Where(x => x.TenantId == c.TenantId && productIds.Contains(x.ProductId)).ToListAsync(ct); db.CompanyProductPrices.RemoveRange(old); db.CompanyProductPrices.AddRange(c.Products.Select(x => new CompanyProductPrice { TenantId = c.TenantId, ProductId = x.ProductId, ContractPrice = x.ContractPrice, ValidFrom = c.StartsAt, ValidTo = c.EndsAt, CreatedBy = actor }));
    }
    private static ContractRowDto MapRow(CompanyContract c, Company company, IEnumerable<Order> orders, IReadOnlyDictionary<Guid, Product> products, IReadOnlyDictionary<Guid, string> staff, DateTime now)
    {
        var spend = orders.Where(x => x.CreatedAt >= c.StartsAt && x.CreatedAt <= c.EndsAt).Sum(x => x.Total); var consumption = c.AnnualValue <= 0 ? 0 : Math.Round(Math.Min(100, spend / c.AnnualValue * 100m), 1); var productRows = c.Products.Where(x => products.ContainsKey(x.ProductId)).Select(x => ProductDto(x, products[x.ProductId])).ToList(); var saving = productRows.Count == 0 ? 0 : Math.Round(productRows.Average(x => x.CustomerSavingPercent), 1); var status = c.Status.ToString(); if (c.Status == CompanyContractStatus.Active && c.EndsAt <= now.AddDays(c.ExpiryAlertDays)) status = c.EndsAt < now ? "Expired" : "Expiring"; return new(c.Id, c.Number, c.CompanyId, company.LegalName, c.Type.ToString(), c.AnnualValue, consumption, c.StartsAt, c.EndsAt, status, c.Products.Count, saving, company.AssignedSalesRepUserId is Guid rep ? Name(staff, rep) : null);
    }
    private static ContractProductDto ProductDto(CompanyContractProduct x, Product p) { var saving = p.BasePrice - x.ContractPrice; var savingPct = p.BasePrice <= 0 ? 0 : Math.Round(saving / p.BasePrice * 100m, 1); var margin = x.ContractPrice <= 0 ? 0 : Math.Round((x.ContractPrice - p.CostPrice) / x.ContractPrice * 100m, 1); return new(x.ProductId, p.Sku, p.NameAr, p.BasePrice, p.CostPrice, x.ContractPrice, x.EstimatedAnnualQuantity, saving, savingPct, margin); }
    private void Audit(Guid actor, Guid tenant, string action, Guid id, string? data) => db.AuditLogs.Add(new AuditLog { UserId = actor, TenantId = tenant, Action = action, EntityType = nameof(CompanyContract), EntityId = id.ToString(), DataJson = data });
    private static T Parse<T>(string value, string error) where T : struct, Enum => Enum.TryParse<T>(value, true, out var result) ? result : throw ApiException.BadRequest(error);
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Name(IReadOnlyDictionary<Guid, string> users, Guid id) => users.TryGetValue(id, out var name) ? name : "مستخدم غير معروف";
}
