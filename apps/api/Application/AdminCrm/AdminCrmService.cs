using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminCrm;

public sealed class AdminCrmService(AppDbContext db)
{
    public async Task<CrmDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var companies = await db.Companies.AsNoTracking().Include(x => x.Tenant).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var staff = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff && x.IsActive).OrderBy(x => x.FullName).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().ToListAsync(ct);
        var tasks = await db.CrmTasks.AsNoTracking().ToListAsync(ct);
        var staffMap = staff.ToDictionary(x => x.Id, x => x.FullName);
        var rows = companies.Select(c => Row(c, orders.Where(x => x.TenantId == c.TenantId), tasks.Where(x => x.CompanyId == c.Id), staffMap)).ToList();
        var outstanding = companies.Sum(x => Math.Max(0, x.CreditUsed));
        return new(new(companies.Count, companies.Count(x => x.Tenant.Status == TenantStatus.Active), companies.Count(x => x.CustomerStage is CustomerStage.Lead or CustomerStage.Qualified or CustomerStage.Proposal or CustomerStage.Negotiation), companies.Count(x => x.CustomerStage == CustomerStage.AtRisk), orders.Where(x => x.Status != OrderStatus.Cancelled).Sum(x => x.Total), outstanding, tasks.Count(x => x.Status is CrmTaskStatus.Open or CrmTaskStatus.InProgress)), rows,
            staff.Select(x => new CrmStaffDto(x.Id, x.FullName, x.JobTitle)).ToList(),
            await db.Products.AsNoTracking().Where(x => x.Status == ProductStatus.Active).OrderBy(x => x.NameAr).Select(x => new CrmProductOptionDto(x.Id, x.Sku, x.NameAr, x.BasePrice)).ToListAsync(ct));
    }

    public async Task<CrmCompanyDetailDto> DetailAsync(Guid companyId, CancellationToken ct = default)
    {
        var company = await db.Companies.AsNoTracking().Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == companyId, ct) ?? throw ApiException.NotFound("الشركة غير موجودة");
        var tenantId = company.TenantId;
        var branches = await db.CompanyBranches.AsNoTracking().Where(x => x.CompanyId == companyId).OrderByDescending(x => x.IsMain).ThenBy(x => x.Name).ToListAsync(ct);
        var users = await db.Users.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.FullName).ToListAsync(ct);
        var documents = await db.CompanyDocuments.AsNoTracking().Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var activities = await db.CrmActivities.AsNoTracking().Where(x => x.CompanyId == companyId).OrderByDescending(x => x.OccurredAt).ToListAsync(ct);
        var tasks = await db.CrmTasks.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.Status).ThenBy(x => x.DueAt).ToListAsync(ct);
        var stages = await db.CustomerStageHistories.AsNoTracking().Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().Include(x => x.Items).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var quotes = await db.CustomerQuotes.AsNoTracking().Include(x => x.Versions).Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var contracts = await db.CompanyContracts.AsNoTracking().Where(x => x.TenantId == tenantId && x.CompanyId == companyId).OrderByDescending(x => x.EndsAt).ToListAsync(ct);
        var prices = await db.CompanyProductPrices.AsNoTracking().Where(x => x.TenantId == tenantId).ToListAsync(ct);
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.TenantId == tenantId).OrderBy(x => x.IssuedAt).ToListAsync(ct);
        var tickets = await db.SupportTickets.AsNoTracking().Where(x => x.TenantId == tenantId).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var products = await db.Products.AsNoTracking().Where(x => x.Status == ProductStatus.Active).ToListAsync(ct);
        var links = await db.ProductLinks.AsNoTracking().Where(x => x.Type == ProductLinkType.Related).ToListAsync(ct);
        var allPeople = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff || x.TenantId == tenantId).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var staffMap = allPeople;

        var validOrders = orders.Where(x => x.Status != OrderStatus.Cancelled).ToList();
        var topProducts = validOrders.SelectMany(x => x.Items).GroupBy(x => new { x.ProductId, x.Sku, Product = x.NameAr }).Select(g => new CrmTopProductDto(g.Key.ProductId, g.Key.Sku, g.Key.Product, g.Sum(x => x.Quantity), g.Sum(x => x.LineTotal))).OrderByDescending(x => x.Sales).Take(10).ToList();
        var purchased = topProducts.Select(x => x.ProductId).ToHashSet();
        var linkedIds = links.Where(x => purchased.Contains(x.ProductId) && !purchased.Contains(x.LinkedProductId)).Select(x => x.LinkedProductId).Distinct().Take(6).ToList();
        var upsellProducts = products.Where(x => linkedIds.Contains(x.Id)).ToList();
        if (upsellProducts.Count < 6) upsellProducts.AddRange(products.Where(x => x.IsFeatured && !purchased.Contains(x.Id) && upsellProducts.All(u => u.Id != x.Id)).Take(6 - upsellProducts.Count));

        decimal balance = 0;
        var statement = invoices.Select(i => { balance += i.Total - i.PaidAmount; return new CrmStatementLineDto(i.Id, i.Number, i.Type.ToString(), i.IssuedAt, i.DueAt, i.Total, i.PaidAmount, balance, i.Status.ToString()); }).Reverse().ToList();
        return new(
            Row(company, orders, tasks, staffMap),
            branches.Select(x => new CrmBranchDto(x.Id, x.Name, x.Governorate, x.City, x.AddressLine, x.Phone, x.IsMain)).ToList(),
            users.Select(x => new CrmUserDto(x.Id, x.FullName, x.Phone, x.Email, x.JobTitle, x.Department, x.IsActive, x.CreatedAt)).ToList(),
            documents.Select(x => new CrmDocumentDto(x.Id, x.Type.ToString(), x.FileName, x.StoragePath, x.ReviewStatus.ToString(), x.RejectionReason, x.CreatedAt)).ToList(),
            activities.Select(x => new CrmActivityDto(x.Id, x.Type.ToString(), x.Subject, x.Details, x.OccurredAt, x.NextFollowUpAt, Name(allPeople, x.ActorUserId), x.ContactUserId is null ? null : Name(allPeople, x.ContactUserId.Value))).ToList(),
            tasks.Select(x => new CrmTaskDto(x.Id, x.Title, x.Description, Name(allPeople, x.AssignedToUserId), x.DueAt, x.Status.ToString(), x.Priority.ToString(), x.CompletedAt)).ToList(),
            stages.Select(x => new CrmStageDto(x.Id, x.FromStage.ToString(), x.ToStage.ToString(), x.Reason, Name(allPeople, x.ChangedByUserId), x.CreatedAt)).ToList(),
            quotes.Select(x => new CrmCommercialDocumentDto(x.Id, x.Number, x.Status.ToString(), x.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.Total ?? 0, x.CreatedAt)).ToList(),
            orders.Select(x => new CrmCommercialDocumentDto(x.Id, x.Number, x.Status.ToString(), x.Total, x.CreatedAt)).ToList(),
            validOrders.Count == 0 ? 0 : Math.Round(validOrders.Average(x => x.Total), 2), topProducts,
            upsellProducts.Select(x => new CrmUpsellDto(x.Id, x.Sku, x.NameAr, linkedIds.Contains(x.Id) ? "منتج مكمل لمشتريات الشركة" : "منتج رائج لم تشتره الشركة بعد", x.BasePrice)).ToList(),
            contracts.Select(x => new CrmContractDto(x.Id, x.Number, x.StartsAt, x.EndsAt, x.Status.ToString(), x.PaymentTermsDays, x.CreditLimit, x.AutoRenew)).ToList(),
            prices.Join(products, p => p.ProductId, p => p.Id, (price, product) => new CrmSpecialPriceDto(product.Id, product.Sku, product.NameAr, product.BasePrice, price.ContractPrice, price.ValidFrom, price.ValidTo)).ToList(),
            statement,
            tickets.Select(x => new CrmTicketDto(x.Id, x.Number, x.Subject, x.Type.ToString(), x.Priority.ToString(), x.Status.ToString(), x.CreatedAt, x.ResolvedAt)).ToList());
    }

    public async Task<Guid> CreateCompanyAsync(Guid actor, SaveCrmCompanyDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        await ValidateSalesRep(dto.AssignedSalesRepUserId, ct);
        if (await db.Companies.AnyAsync(x => x.LegalName == dto.LegalName.Trim(), ct)) throw ApiException.Conflict("اسم الشركة مسجل بالفعل");
        var tenant = new Tenant { Name = dto.LegalName.Trim(), NameEn = Empty(dto.LegalNameEn), Status = TenantStatus.PendingVerification };
        var company = new Company { TenantId = tenant.Id, Tenant = tenant };
        Apply(company, dto);
        db.Companies.Add(company);
        db.CompanyBranches.Add(new CompanyBranch { TenantId = tenant.Id, CompanyId = company.Id, Name = "المقر الرئيسي", Governorate = Empty(dto.Governorate), City = Empty(dto.City), AddressLine = Empty(dto.AddressLine), Phone = dto.Phone.Trim(), IsMain = true });
        if (!string.IsNullOrWhiteSpace(dto.PrimaryContactName) && !string.IsNullOrWhiteSpace(dto.PrimaryContactPhone))
        {
            var phone = dto.PrimaryContactPhone.Trim();
            if (await db.Users.AnyAsync(x => x.Phone == phone, ct)) throw ApiException.Conflict("هاتف مسؤول التواصل مسجل بالفعل");
            db.Users.Add(new User { TenantId = tenant.Id, FullName = dto.PrimaryContactName.Trim(), Phone = phone, Email = Empty(dto.PrimaryContactEmail), IsActive = true, JobTitle = "مسؤول التواصل" });
        }
        Audit(actor, tenant.Id, "crm.company.create", company.Id, dto.LegalName);
        await db.SaveChangesAsync(ct);
        return company.Id;
    }

    public async Task UpdateCompanyAsync(Guid actor, Guid id, SaveCrmCompanyDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        await ValidateSalesRep(dto.AssignedSalesRepUserId, ct);
        var company = await db.Companies.Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الشركة غير موجودة");
        if (await db.Companies.AnyAsync(x => x.Id != id && x.LegalName == dto.LegalName.Trim(), ct)) throw ApiException.Conflict("اسم الشركة مسجل بالفعل");
        Apply(company, dto); company.Tenant.Name = company.LegalName; company.Tenant.NameEn = company.LegalNameEn;
        Audit(actor, company.TenantId, "crm.company.update", company.Id, dto.LegalName); await db.SaveChangesAsync(ct);
    }

    public async Task ChangeStageAsync(Guid actor, Guid id, ChangeCustomerStageDto dto, CancellationToken ct = default)
    {
        var company = await Find(id, ct); var next = Parse<CustomerStage>(dto.Stage, "مرحلة العميل غير صحيحة");
        if (company.CustomerStage == next) return;
        db.CustomerStageHistories.Add(new CustomerStageHistory { CompanyId = id, FromStage = company.CustomerStage, ToStage = next, Reason = Empty(dto.Reason), ChangedByUserId = actor });
        company.CustomerStage = next; Audit(actor, company.TenantId, "crm.company.stage", id, $"{next}: {dto.Reason}"); await db.SaveChangesAsync(ct);
    }

    public async Task AddActivityAsync(Guid actor, Guid id, AddCrmActivityDto dto, CancellationToken ct = default)
    {
        _ = await Find(id, ct); if (string.IsNullOrWhiteSpace(dto.Subject)) throw ApiException.BadRequest("موضوع النشاط مطلوب");
        var type = Parse<CrmActivityType>(dto.Type, "نوع النشاط غير صحيح");
        db.CrmActivities.Add(new CrmActivity { CompanyId = id, Type = type, Subject = dto.Subject.Trim(), Details = Empty(dto.Details), OccurredAt = dto.OccurredAt, NextFollowUpAt = dto.NextFollowUpAt, ActorUserId = actor, ContactUserId = dto.ContactUserId }); await db.SaveChangesAsync(ct);
    }

    public async Task AddTaskAsync(Guid actor, Guid id, AddCrmTaskDto dto, CancellationToken ct = default)
    {
        _ = await Find(id, ct); if (string.IsNullOrWhiteSpace(dto.Title) || !await db.Users.AnyAsync(x => x.Id == dto.AssignedToUserId && x.IsActive, ct)) throw ApiException.BadRequest("عنوان المهمة أو الموظف غير صالح");
        db.CrmTasks.Add(new CrmTask { CompanyId = id, Title = dto.Title.Trim(), Description = Empty(dto.Description), AssignedToUserId = dto.AssignedToUserId, DueAt = dto.DueAt, Priority = Parse<CrmTaskPriority>(dto.Priority, "أولوية المهمة غير صحيحة"), CreatedBy = actor }); await db.SaveChangesAsync(ct);
    }

    public async Task CompleteTaskAsync(Guid actor, Guid taskId, CancellationToken ct = default)
    {
        var task = await db.CrmTasks.FirstOrDefaultAsync(x => x.Id == taskId, ct) ?? throw ApiException.NotFound("المهمة غير موجودة");
        if (task.Status == CrmTaskStatus.Completed) return; task.Status = CrmTaskStatus.Completed; task.CompletedAt = DateTime.UtcNow; task.UpdatedBy = actor; await db.SaveChangesAsync(ct);
    }

    public async Task AddBranchAsync(Guid actor, Guid id, AddCrmBranchDto dto, CancellationToken ct = default)
    {
        var company = await Find(id, ct); if (string.IsNullOrWhiteSpace(dto.Name)) throw ApiException.BadRequest("اسم الفرع مطلوب");
        if (dto.IsMain) foreach (var branch in await db.CompanyBranches.Where(x => x.CompanyId == id && x.IsMain).ToListAsync(ct)) branch.IsMain = false;
        db.CompanyBranches.Add(new CompanyBranch { TenantId = company.TenantId, CompanyId = id, Name = dto.Name.Trim(), Governorate = Empty(dto.Governorate), City = Empty(dto.City), AddressLine = Empty(dto.AddressLine), Phone = Empty(dto.Phone), IsMain = dto.IsMain, CreatedBy = actor }); await db.SaveChangesAsync(ct);
    }

    public async Task ReviewDocumentAsync(Guid actor, Guid companyId, Guid documentId, ReviewCrmDocumentDto dto, CancellationToken ct = default)
    {
        _ = await Find(companyId, ct); var document = await db.CompanyDocuments.FirstOrDefaultAsync(x => x.Id == documentId && x.CompanyId == companyId, ct) ?? throw ApiException.NotFound("المستند غير موجود");
        document.ReviewStatus = Parse<DocumentReviewStatus>(dto.Status, "حالة المراجعة غير صحيحة"); document.RejectionReason = document.ReviewStatus == DocumentReviewStatus.Rejected ? Empty(dto.RejectionReason) : null; document.UpdatedBy = actor; await db.SaveChangesAsync(ct);
    }

    public async Task ReplacePricesAsync(Guid actor, Guid id, ReplaceCrmPricesDto dto, CancellationToken ct = default)
    {
        var company = await Find(id, ct); if (dto.Prices.Select(x => x.ProductId).Distinct().Count() != dto.Prices.Count || dto.Prices.Any(x => x.ContractPrice <= 0 || x.ValidTo < x.ValidFrom)) throw ApiException.BadRequest("قائمة الأسعار الخاصة غير صالحة");
        var productIds = dto.Prices.Select(x => x.ProductId).ToList(); if (await db.Products.CountAsync(x => productIds.Contains(x.Id), ct) != productIds.Count) throw ApiException.BadRequest("أحد المنتجات غير موجود");
        db.CompanyProductPrices.RemoveRange(await db.CompanyProductPrices.Where(x => x.TenantId == company.TenantId).ToListAsync(ct));
        db.CompanyProductPrices.AddRange(dto.Prices.Select(x => new CompanyProductPrice { TenantId = company.TenantId, ProductId = x.ProductId, ContractPrice = x.ContractPrice, ValidFrom = x.ValidFrom, ValidTo = x.ValidTo, CreatedBy = actor })); await db.SaveChangesAsync(ct);
    }

    public async Task UpdateCreditAsync(Guid actor, Guid id, UpdateCreditLimitDto dto, CancellationToken ct = default)
    {
        var company = await Find(id, ct); if (dto.CreditLimit < company.CreditUsed || dto.CreditLimit < 0) throw ApiException.Conflict("حد الائتمان لا يمكن أن يقل عن الرصيد المستخدم"); company.CreditLimit = dto.CreditLimit; company.UpdatedBy = actor; await db.SaveChangesAsync(ct);
    }

    public async Task ChangeStatusAsync(Guid actor, Guid id, ChangeCompanyStatusDto dto, CancellationToken ct = default)
    {
        var company = await db.Companies.Include(x => x.Tenant).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الشركة غير موجودة");
        var status = Parse<TenantStatus>(dto.Status, "حالة الشركة غير صحيحة"); company.Tenant.Status = status; Audit(actor, company.TenantId, status == TenantStatus.Suspended ? "crm.company.suspend" : "crm.company.status", id, dto.Reason); await db.SaveChangesAsync(ct);
    }

    private async Task<Company> Find(Guid id, CancellationToken ct) => await db.Companies.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("الشركة غير موجودة");
    private async Task ValidateSalesRep(Guid? id, CancellationToken ct) { if (id is Guid value && !await db.Users.AnyAsync(x => x.Id == value && x.IsPlatformStaff && x.IsActive, ct)) throw ApiException.BadRequest("مندوب المبيعات غير صالح"); }
    private static CrmCompanyRowDto Row(Company c, IEnumerable<Order> orders, IEnumerable<CrmTask> tasks, IReadOnlyDictionary<Guid, string> staff)
    {
        var valid = orders.Where(x => x.Status != OrderStatus.Cancelled).ToList();
        return new(c.Id, c.TenantId, c.LegalName, c.LegalNameEn, c.Phone, c.Email, c.CommercialRegistrationNo, c.TaxCardNo, c.Governorate, c.City, c.AddressLine, c.Industry, c.Sector, c.Activity, c.Classification.ToString(), c.Size.ToString(), c.CustomerStage.ToString(), c.Tenant.Status.ToString(), c.AssignedSalesRepUserId, c.AssignedSalesRepUserId is Guid id && staff.TryGetValue(id, out var name) ? name : null, c.LeadSource, c.CreditLimit, c.CreditUsed, valid.Sum(x => x.Total), valid.Count, tasks.Count(x => x.Status is CrmTaskStatus.Open or CrmTaskStatus.InProgress), c.CreatedAt);
    }
    private void Apply(Company c, SaveCrmCompanyDto d)
    {
        c.LegalName = d.LegalName.Trim(); c.LegalNameEn = Empty(d.LegalNameEn); c.Phone = d.Phone.Trim(); c.Email = Empty(d.Email); c.CommercialRegistrationNo = Empty(d.CommercialRegistrationNo); c.TaxCardNo = Empty(d.TaxCardNo); c.Governorate = Empty(d.Governorate); c.City = Empty(d.City); c.AddressLine = Empty(d.AddressLine); c.Industry = Empty(d.Industry); c.Sector = Empty(d.Sector); c.Activity = Empty(d.Activity); c.Classification = Parse<CompanyClassification>(d.Classification, "تصنيف الشركة غير صحيح"); c.Size = Parse<CompanySize>(d.Size, "حجم الشركة غير صحيح"); c.AssignedSalesRepUserId = d.AssignedSalesRepUserId; c.LeadSource = Empty(d.LeadSource); c.CreditLimit = Math.Max(0, d.CreditLimit);
    }
    private static void Validate(SaveCrmCompanyDto d) { if (string.IsNullOrWhiteSpace(d.LegalName) || string.IsNullOrWhiteSpace(d.Phone)) throw ApiException.BadRequest("اسم الشركة ورقم الهاتف مطلوبان"); }
    private void Audit(Guid actor, Guid? tenantId, string action, Guid entityId, string? data) => db.AuditLogs.Add(new AuditLog { UserId = actor, TenantId = tenantId, Action = action, EntityType = nameof(Company), EntityId = entityId.ToString(), DataJson = data });
    private static T Parse<T>(string value, string error) where T : struct, Enum => Enum.TryParse<T>(value, true, out var result) ? result : throw ApiException.BadRequest(error);
    private static string? Empty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Name(IReadOnlyDictionary<Guid, string> people, Guid id) => people.TryGetValue(id, out var name) ? name : "مستخدم غير معروف";
}
