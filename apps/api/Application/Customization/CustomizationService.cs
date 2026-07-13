using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Customization;

public sealed class CustomizationService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment environment)
{
    private static readonly Dictionary<string, string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png", ["image/jpeg"] = ".jpg", ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg", ["application/pdf"] = ".pdf",
        ["application/postscript"] = ".ai", ["application/illustrator"] = ".ai",
    };

    public async Task<IReadOnlyList<CustomTemplateSummaryDto>> TemplatesAsync(CancellationToken ct = default) =>
        await db.CustomProductTemplates.AsNoTracking().Where(t => t.IsActive && t.Product.Status == ProductStatus.Active)
            .OrderBy(t => t.Product.NameAr).Select(t => new CustomTemplateSummaryDto(t.Id, t.ProductId, t.Product.Sku,
                t.NameAr, t.Product.Images.OrderByDescending(i => i.IsPrimary).Select(i => i.Path).FirstOrDefault(),
                t.Product.BasePrice, t.SetupFee, t.MinQuantity, t.LeadTimeDays)).ToListAsync(ct);

    public async Task<CustomTemplateDto> TemplateAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.CustomProductTemplates.AsNoTracking().Include(x => x.Product).ThenInclude(p => p.Images)
            .Include(x => x.Options).Include(x => x.PrintMethods).Include(x => x.Materials).Include(x => x.Colors).Include(x => x.Sizes)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct) ?? throw ApiException.NotFound("قالب المنتج المخصص غير موجود");
        return new(t.Id, t.ProductId, t.Product.Sku, t.NameAr, t.DescriptionAr,
            t.Product.Images.OrderByDescending(i => i.IsPrimary).Select(i => i.Path).FirstOrDefault(), t.Product.BasePrice,
            t.SetupFee, t.MinQuantity, t.LeadTimeDays,
            t.Options.OrderBy(x => x.SortOrder).Select(x => Choice(x.Id, x.Code, x.NameAr, null, x.PriceAdjustment)).ToList(),
            t.PrintMethods.OrderBy(x => x.SortOrder).Select(x => Choice(x.Id, x.Code, x.NameAr, x.DescriptionAr, x.UnitPriceAdjustment)).ToList(),
            t.Materials.OrderBy(x => x.SortOrder).Select(x => Choice(x.Id, x.Code, x.NameAr, null, x.UnitPriceAdjustment)).ToList(),
            t.Colors.OrderBy(x => x.SortOrder).Select(x => Choice(x.Id, x.Code, x.NameAr, null, x.UnitPriceAdjustment, x.Hex)).ToList(),
            t.Sizes.OrderBy(x => x.SortOrder).Select(x => Choice(x.Id, x.Code, x.NameAr, null, x.UnitPriceAdjustment)).ToList());
    }

    public async Task<CustomRequestDto> CreateAsync(Guid userId, CreateCustomRequestForm form, CancellationToken ct = default)
    {
        var tenantId = TenantId();
        var template = await db.CustomProductTemplates.Include(t => t.Product).Include(t => t.Options).Include(t => t.PrintMethods)
            .Include(t => t.Materials).Include(t => t.Colors).Include(t => t.Sizes)
            .FirstOrDefaultAsync(t => t.Id == form.TemplateId && t.IsActive, ct) ?? throw ApiException.NotFound("قالب المنتج المخصص غير موجود");
        var placement = template.Options.SingleOrDefault(x => x.Id == form.PlacementId) ?? throw ApiException.BadRequest("موضع الطباعة غير صالح");
        var method = template.PrintMethods.SingleOrDefault(x => x.Id == form.PrintMethodId) ?? throw ApiException.BadRequest("طريقة الطباعة غير صالحة");
        var material = template.Materials.SingleOrDefault(x => x.Id == form.MaterialId) ?? throw ApiException.BadRequest("الخامة غير صالحة");
        var color = template.Colors.SingleOrDefault(x => x.Id == form.ColorId) ?? throw ApiException.BadRequest("اللون غير صالح");
        var size = template.Sizes.SingleOrDefault(x => x.Id == form.SizeId) ?? throw ApiException.BadRequest("المقاس غير صالح");
        var minimum = Math.Max(template.MinQuantity, method.MinQuantity);
        if (form.Quantity < minimum) throw ApiException.BadRequest($"الحد الأدنى لهذه الخيارات {minimum} قطعة");
        if (form.DesignServiceRequested && string.IsNullOrWhiteSpace(form.Objective))
            throw ApiException.BadRequest("هدف التصميم مطلوب عند طلب خدمة التصميم");
        if (!form.DesignServiceRequested && form.DesignFile is null)
            throw ApiException.BadRequest("ارفع ملف التصميم أو اختر خدمة التصميم من مهندسيتو");
        if (form.Logo is null && form.ExistingLogoAssetId is null) throw ApiException.BadRequest("شعار الشركة مطلوب");
        if (form.PrintWidthCm is <= 0 or > 100 || form.PrintHeightCm is <= 0 or > 100)
            throw ApiException.BadRequest("أبعاد الطباعة يجب أن تكون بين 0 و100 سم");
        if (form.PrintColorCount is < 1 or > 8) throw ApiException.BadRequest("عدد ألوان الطباعة يجب أن يكون بين 1 و8");

        var unitPrice = template.Product.BasePrice + placement.PriceAdjustment + method.UnitPriceAdjustment
            + material.UnitPriceAdjustment + color.UnitPriceAdjustment + size.UnitPriceAdjustment + (form.PrintColorCount - 1) * 2m;
        var request = new CustomProductRequest
        {
            TenantId = tenantId, UserId = userId, TemplateId = template.Id,
            Number = $"CPR-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Status = CustomRequestStatus.AwaitingQuote, DesignServiceRequested = form.DesignServiceRequested,
            CustomerNote = Clean(form.CustomerNote, 1000), EstimatedTotal = unitPrice * form.Quantity + template.SetupFee,
            EstimatedLeadTimeDays = template.LeadTimeDays,
        };
        request.Items.Add(new CustomRequestItem { TenantId = tenantId, Quantity = form.Quantity, OptionId = placement.Id,
            PrintMethodId = method.Id, MaterialId = material.Id, ColorId = color.Id, SizeId = size.Id,
            CustomText = Clean(form.CustomText, 300), PrintWidthCm = form.PrintWidthCm, PrintHeightCm = form.PrintHeightCm,
            PrintColorCount = form.PrintColorCount, EstimatedUnitPrice = unitPrice });
        if (form.DesignServiceRequested)
            request.DesignBrief = new DesignBrief { TenantId = tenantId, Objective = CleanRequired(form.Objective, 1000),
                Audience = Clean(form.Audience, 500), BrandGuidelines = Clean(form.BrandGuidelines, 1000),
                PreferredColors = Clean(form.PreferredColors, 300), RequiredText = Clean(form.RequiredText, 1000), DesiredDate = form.DesiredDate };
        if (form.Logo is not null)
            request.LogoAssets.Add(await SaveAssetAsync(request.Id, form.Logo, false, ct));
        else
        {
            var existing = await db.LogoAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == form.ExistingLogoAssetId && !a.IsDesignFile, ct)
                ?? throw ApiException.BadRequest("الشعار المحفوظ غير صالح");
            request.LogoAssets.Add(new LogoAsset { TenantId = tenantId, RequestId = request.Id, OriginalName = existing.OriginalName,
                StoredPath = existing.StoredPath, ContentType = existing.ContentType, SizeBytes = existing.SizeBytes });
        }
        if (form.DesignFile is not null) request.LogoAssets.Add(await SaveAssetAsync(request.Id, form.DesignFile, true, ct));
        db.CustomProductRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return await RequestAsync(userId, request.Id, ct);
    }

    public async Task<IReadOnlyList<CustomRequestListDto>> RequestsAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await db.CustomProductRequests.AsNoTracking().Where(r => r.UserId == userId)
            .Select(r => new { r.Id, r.Number, Name = r.Template.Product.NameAr, r.Status, Quantity = r.Items.Select(i => i.Quantity).First(),
                Total = r.QuotedTotal ?? r.EstimatedTotal, r.CreatedAt }).OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        return rows.Select(r => new CustomRequestListDto(r.Id, r.Number, r.Name, r.Status.ToString(), r.Quantity, r.Total, r.CreatedAt, Progress(r.Status))).ToList();
    }

    public async Task<IReadOnlyList<SavedLogoDto>> SavedLogosAsync(CancellationToken ct = default)
    {
        var assets = await db.LogoAssets.AsNoTracking().Where(a => !a.IsDesignFile).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
        return assets.GroupBy(a => new { a.OriginalName, a.SizeBytes }).Select(g => g.First()).Take(20)
            .Select(a => new SavedLogoDto(a.Id, a.OriginalName, a.ContentType, a.SizeBytes, a.CreatedAt,
                $"/api/custom-products/files/{a.Id}")).ToList();
    }

    public async Task<CustomRequestDto> RequestAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var request = await QueryRequest().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        return Map(request);
    }

    public async Task<CustomRequestDto> SaveBriefAsync(Guid userId, Guid id, SaveDesignBriefDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.DesignBrief).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status is CustomRequestStatus.InProduction or CustomRequestStatus.Completed or CustomRequestStatus.Cancelled)
            throw ApiException.Conflict("لا يمكن تعديل موجز التصميم في الحالة الحالية");
        var brief = request.DesignBrief;
        if (brief is null)
        {
            brief = new DesignBrief { TenantId = TenantId(), RequestId = request.Id };
            db.DesignBriefs.Add(brief);
        }
        brief.Objective = CleanRequired(dto.Objective, 1000); brief.Audience = Clean(dto.Audience, 500);
        brief.BrandGuidelines = Clean(dto.BrandGuidelines, 1000); brief.PreferredColors = Clean(dto.PreferredColors, 300);
        brief.RequiredText = Clean(dto.RequiredText, 1000); brief.DesiredDate = dto.DesiredDate;
        await db.SaveChangesAsync(ct); return await RequestAsync(userId, id, ct);
    }

    public async Task<CustomRequestDto> AddCommentAsync(Guid userId, Guid id, AddDesignCommentDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        db.DesignComments.Add(new DesignComment { TenantId = TenantId(), RequestId = request.Id, UserId = userId, Body = CleanRequired(dto.Body, 1500) });
        await db.SaveChangesAsync(ct); return await RequestAsync(userId, id, ct);
    }

    public async Task<CustomRequestDto> RespondToQuoteAsync(Guid userId, Guid id, bool accept, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.LogoAssets).FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status != CustomRequestStatus.Quoted || request.QuotedTotal is null || request.QuoteExpiresAt <= DateTime.UtcNow)
            throw ApiException.Conflict("عرض السعر غير متاح أو انتهت صلاحيته");
        request.Status = accept ? request.DesignServiceRequested ? CustomRequestStatus.DesignInProgress : CustomRequestStatus.DesignApproved : CustomRequestStatus.Rejected;
        await db.SaveChangesAsync(ct); return await RequestAsync(userId, id, ct);
    }

    public async Task<CustomRequestDto> DesignDecisionAsync(Guid userId, Guid id, DesignDecisionDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.DesignVersions).Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct) ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status != CustomRequestStatus.AwaitingDesignApproval) throw ApiException.Conflict("لا يوجد تصميم بانتظار الاعتماد");
        var latest = request.DesignVersions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latest is null || latest.Id != dto.VersionId) throw ApiException.BadRequest("يجب اتخاذ القرار على أحدث نسخة");
        if (!Enum.TryParse<DesignApprovalDecision>(dto.Decision, true, out var decision) || decision == DesignApprovalDecision.Pending)
            throw ApiException.BadRequest("قرار الاعتماد غير صالح");
        db.DesignApprovals.Add(new DesignApproval { TenantId = TenantId(), RequestId = request.Id, VersionId = latest.Id,
            UserId = userId, Decision = decision, Note = Clean(dto.Note, 1000) });
        request.Status = decision switch { DesignApprovalDecision.Approved => CustomRequestStatus.DesignApproved,
            DesignApprovalDecision.Rejected => CustomRequestStatus.Rejected, _ => CustomRequestStatus.DesignInProgress };
        await db.SaveChangesAsync(ct); return await RequestAsync(userId, id, ct);
    }

    public async Task StartForOrderAsync(IEnumerable<Guid> requestIds, Guid orderId, bool requiresApproval, CancellationToken ct = default)
    {
        var ids = requestIds.Distinct().ToList();
        if (ids.Count == 0) return;
        var requests = await db.CustomProductRequests.Include(r => r.ProductionJob).Where(r => ids.Contains(r.Id)).ToListAsync(ct);
        if (requests.Count != ids.Count || requests.Any(r => r.Status != CustomRequestStatus.AwaitingCheckout))
            throw ApiException.Conflict("أحد طلبات التخصيص غير جاهز لإتمام الطلب");
        foreach (var request in requests)
        {
            request.OrderId = orderId;
            if (requiresApproval) request.Status = CustomRequestStatus.AwaitingOrderApproval;
            else CreateProductionJob(request);
        }
    }

    public async Task<(string Path, string ContentType, string Name)> AssetAsync(Guid assetId, CancellationToken ct = default)
    {
        var asset = await db.LogoAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assetId, ct)
            ?? throw ApiException.NotFound("الملف غير موجود");
        return (SafeStoredPath(asset.StoredPath), asset.ContentType, asset.OriginalName);
    }

    public async Task<(string Path, string ContentType, string Name)> MockupAsync(Guid mockupId, CancellationToken ct = default)
    {
        var file = await db.DesignMockups.AsNoTracking().FirstOrDefaultAsync(a => a.Id == mockupId, ct)
            ?? throw ApiException.NotFound("المعاينة غير موجودة");
        return (SafeStoredPath(file.StoredPath), file.ContentType, file.OriginalName);
    }

    public async Task<(string Path, string ContentType, string Name)> SampleFileAsync(Guid sampleId, CancellationToken ct = default)
    {
        var file = await db.ProductionSamples.AsNoTracking().FirstOrDefaultAsync(a => a.Id == sampleId, ct)
            ?? throw ApiException.NotFound("ملف العينة غير موجود");
        return (SafeStoredPath(file.StoredPath), file.ContentType, file.OriginalName);
    }

    public async Task<CustomRequestDto> AdminRequestAsync(Guid id, CancellationToken ct = default)
    {
        var request = await QueryRequest().AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        return Map(request);
    }

    public async Task<CustomRequestDto> SetQuoteAsync(Guid id, SetCustomQuoteDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status is not (CustomRequestStatus.AwaitingQuote or CustomRequestStatus.Quoted))
            throw ApiException.Conflict("لا يمكن تسعير الطلب في الحالة الحالية");
        if (dto.Total <= 0 || dto.ExpiresAt <= DateTime.UtcNow || dto.LeadTimeDays is < 1 or > 120) throw ApiException.BadRequest("بيانات عرض السعر غير صالحة");
        request.QuotedTotal = dto.Total; request.QuoteExpiresAt = dto.ExpiresAt; request.EstimatedLeadTimeDays = dto.LeadTimeDays; request.Status = CustomRequestStatus.Quoted;
        await db.SaveChangesAsync(ct); return await AdminRequestAsync(id, ct);
    }

    public async Task<CustomRequestDto> PublishDesignAsync(Guid id, Guid designerId, PublishDesignForm form, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.DesignVersions).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status is not (CustomRequestStatus.DesignInProgress or CustomRequestStatus.AwaitingDesignApproval))
            throw ApiException.Conflict("الطلب ليس في مرحلة التصميم");
        var version = new DesignVersion { TenantId = request.TenantId, RequestId = request.Id,
            VersionNumber = request.DesignVersions.Select(v => v.VersionNumber).DefaultIfEmpty().Max() + 1,
            Title = CleanRequired(form.Title, 200), ChangeSummary = Clean(form.ChangeSummary, 1000), CreatedByDesignerId = designerId };
        var stored = await StoreFileAsync(request.TenantId, request.Id, form.File, 20 * 1024 * 1024, ct);
        version.Mockups.Add(new DesignMockup { TenantId = request.TenantId, VersionId = version.Id, OriginalName = form.File.FileName,
            StoredPath = stored.Path, ContentType = stored.ContentType, SizeBytes = form.File.Length, IsPrimary = true });
        db.DesignVersions.Add(version);
        request.Status = CustomRequestStatus.AwaitingDesignApproval;
        await db.SaveChangesAsync(ct);
        return await AdminRequestAsync(id, ct);
    }

    public async Task<CustomRequestDto> UpdateStageAsync(Guid requestId, Guid stageId, UpdateProductionStageDto dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ProductionStageStatus>(dto.Status, true, out var status)) throw ApiException.BadRequest("حالة المرحلة غير صالحة");
        var request = await db.CustomProductRequests.Include(r => r.ProductionJob!).ThenInclude(j => j.Stages)
            .Include(r => r.ProductionJob!).ThenInclude(j => j.QualityChecks)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        var stage = request.ProductionJob?.Stages.FirstOrDefault(s => s.Id == stageId) ?? throw ApiException.NotFound("مرحلة الإنتاج غير موجودة");
        if (stage.Code == "quality" && status == ProductionStageStatus.Completed && !request.ProductionJob!.QualityChecks.Any(q => q.Passed))
            throw ApiException.Conflict("يجب تسجيل فحص جودة ناجح قبل إكمال المرحلة");
        stage.Status = status; stage.Note = Clean(dto.Note, 1000);
        if (status == ProductionStageStatus.InProgress) stage.StartedAt ??= DateTime.UtcNow;
        if (status == ProductionStageStatus.Completed) { stage.StartedAt ??= DateTime.UtcNow; stage.CompletedAt = DateTime.UtcNow; }
        request.Status = request.ProductionJob!.Stages.All(s => s.Id == stage.Id ? status == ProductionStageStatus.Completed : s.Status == ProductionStageStatus.Completed)
            ? CustomRequestStatus.Ready : stage.Code == "quality" && status == ProductionStageStatus.InProgress
                ? CustomRequestStatus.QualityCheck : status == ProductionStageStatus.InProgress ? CustomRequestStatus.InProduction : request.Status;
        await db.SaveChangesAsync(ct); return await AdminRequestAsync(requestId, ct);
    }

    public async Task<CustomRequestDto> AddQualityCheckAsync(Guid requestId, Guid checkedBy, AddQualityCheckDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.ProductionJob)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        var job = request.ProductionJob ?? throw ApiException.Conflict("لم يبدأ أمر الإنتاج بعد");
        db.QualityChecks.Add(new QualityCheck { TenantId = request.TenantId, ProductionJobId = job.Id,
            CheckNameAr = CleanRequired(dto.NameAr, 200), Passed = dto.Passed, CheckedBy = checkedBy,
            CheckedAt = DateTime.UtcNow, Note = Clean(dto.Note, 1000) });
        request.Status = CustomRequestStatus.QualityCheck;
        await db.SaveChangesAsync(ct); return await AdminRequestAsync(requestId, ct);
    }

    public async Task<CustomRequestDto> PublishSampleAsync(Guid requestId, PublishSampleForm form, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.ProductionJob!).ThenInclude(j => j.Samples)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct) ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        var job = request.ProductionJob ?? throw ApiException.Conflict("لم يبدأ أمر الإنتاج بعد");
        if (request.Status is CustomRequestStatus.Ready or CustomRequestStatus.Completed or CustomRequestStatus.Cancelled)
            throw ApiException.Conflict("لا يمكن إضافة عينة في الحالة الحالية");
        var stored = await StoreFileAsync(request.TenantId, request.Id, form.File, 20 * 1024 * 1024, ct);
        var sample = new ProductionSample { TenantId = request.TenantId, ProductionJobId = job.Id,
            VersionNumber = job.Samples.Select(s => s.VersionNumber).DefaultIfEmpty().Max() + 1,
            OriginalName = Path.GetFileName(form.File.FileName), StoredPath = stored.Path, ContentType = stored.ContentType,
            SizeBytes = form.File.Length, Note = Clean(form.Note, 1000) };
        db.ProductionSamples.Add(sample); request.Status = CustomRequestStatus.AwaitingSampleApproval;
        await db.SaveChangesAsync(ct); return await AdminRequestAsync(requestId, ct);
    }

    public async Task<CustomRequestDto> SampleDecisionAsync(Guid userId, Guid requestId, Guid sampleId, SampleDecisionDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(r => r.ProductionJob!).ThenInclude(j => j.Samples)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId, ct) ?? throw ApiException.NotFound("طلب التخصيص غير موجود");
        if (request.Status != CustomRequestStatus.AwaitingSampleApproval) throw ApiException.Conflict("لا توجد عينة بانتظار الاعتماد");
        var latest = request.ProductionJob?.Samples.OrderByDescending(s => s.VersionNumber).FirstOrDefault()
            ?? throw ApiException.NotFound("عينة الإنتاج غير موجودة");
        if (latest.Id != sampleId) throw ApiException.BadRequest("يجب اتخاذ القرار على أحدث عينة");
        if (!Enum.TryParse<SampleApprovalDecision>(dto.Decision, true, out var decision) || decision == SampleApprovalDecision.Pending)
            throw ApiException.BadRequest("قرار العينة غير صالح");
        latest.Decision = decision; latest.DecidedBy = userId; latest.DecidedAt = DateTime.UtcNow; latest.DecisionNote = Clean(dto.Note, 1000);
        request.Status = decision switch { SampleApprovalDecision.Approved => CustomRequestStatus.InProduction,
            SampleApprovalDecision.Rejected => CustomRequestStatus.Rejected, _ => CustomRequestStatus.InProduction };
        await db.SaveChangesAsync(ct); return await RequestAsync(userId, requestId, ct);
    }

    private IQueryable<CustomProductRequest> QueryRequest() => db.CustomProductRequests
        .Include(r => r.Template).ThenInclude(t => t.Product)
        .Include(r => r.Template).ThenInclude(t => t.Options)
        .Include(r => r.Template).ThenInclude(t => t.PrintMethods)
        .Include(r => r.Template).ThenInclude(t => t.Materials)
        .Include(r => r.Template).ThenInclude(t => t.Colors)
        .Include(r => r.Template).ThenInclude(t => t.Sizes)
        .Include(r => r.Items)
        .Include(r => r.LogoAssets).Include(r => r.DesignBrief).Include(r => r.DesignVersions).ThenInclude(v => v.Mockups)
        .Include(r => r.Comments).Include(r => r.Approvals).Include(r => r.ProductionJob!).ThenInclude(j => j.Stages)
        .Include(r => r.ProductionJob!).ThenInclude(j => j.Samples)
        .Include(r => r.ProductionJob!).ThenInclude(j => j.QualityChecks);

    private CustomRequestDto Map(CustomProductRequest r)
    {
        var item = r.Items.Single(); var t = r.Template;
        var placement = t.Options.FirstOrDefault(x => x.Id == item.OptionId)?.NameAr ?? "—";
        var method = t.PrintMethods.FirstOrDefault(x => x.Id == item.PrintMethodId)?.NameAr ?? "—";
        var material = t.Materials.FirstOrDefault(x => x.Id == item.MaterialId)?.NameAr ?? "—";
        var color = t.Colors.FirstOrDefault(x => x.Id == item.ColorId)?.NameAr ?? "—";
        var size = t.Sizes.FirstOrDefault(x => x.Id == item.SizeId)?.NameAr ?? "—";
        var latest = r.Approvals.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.Decision.ToString();
        var production = r.ProductionJob is null ? null : new ProductionDto(r.ProductionJob.Number, r.ProductionJob.ScheduledStart,
            r.ProductionJob.EstimatedCompletion, r.ProductionJob.Stages.OrderBy(s => s.SortOrder)
                .Select(s => new ProductionStageDto(s.Id, s.Code, s.NameAr, s.Status.ToString(), s.SortOrder, s.CompletedAt, s.Note)).ToList(),
            r.ProductionJob.Samples.OrderByDescending(s => s.VersionNumber).Select(s => new ProductionSampleDto(s.Id,
                s.VersionNumber, s.OriginalName, s.ContentType, s.Decision.ToString(), s.Note, s.DecisionNote, s.CreatedAt,
                $"/api/custom-products/samples/{s.Id}")).ToList(),
            r.ProductionJob.QualityChecks.OrderByDescending(q => q.CheckedAt).Select(q => new QualityCheckDto(q.Id,
                q.CheckNameAr, q.Passed, q.CheckedAt, q.Note)).ToList());
        return new(r.Id, r.Number, r.TemplateId, t.Product.NameAr, t.Product.Sku, r.Status.ToString(), item.Quantity,
            placement, method, material, color, size, item.CustomText, item.PrintWidthCm, item.PrintHeightCm,
            item.PrintColorCount, r.DesignServiceRequested, r.CustomerNote,
            item.EstimatedUnitPrice, r.EstimatedTotal, r.QuotedTotal, r.QuoteExpiresAt, r.EstimatedLeadTimeDays,
            r.LogoAssets.Select(a => new CustomAssetDto(a.Id, a.OriginalName, a.ContentType, a.SizeBytes, a.IsDesignFile, $"/api/custom-products/files/{a.Id}")).ToList(),
            r.DesignBrief is null ? null : new DesignBriefDto(r.DesignBrief.Objective, r.DesignBrief.Audience, r.DesignBrief.BrandGuidelines,
                r.DesignBrief.PreferredColors, r.DesignBrief.RequiredText, r.DesignBrief.DesiredDate),
            r.DesignVersions.OrderByDescending(v => v.VersionNumber).Select(v => new DesignVersionDto(v.Id, v.VersionNumber, v.Title,
                v.ChangeSummary, v.CreatedAt, v.Mockups.Select(m => new DesignMockupDto(m.Id, m.OriginalName, m.ContentType, m.IsPrimary,
                    $"/api/custom-products/mockups/{m.Id}")).ToList())).ToList(),
            r.Comments.Where(c => !c.IsInternal).OrderBy(c => c.CreatedAt).Select(c => new DesignCommentDto(c.Id, c.UserId, c.Body, c.CreatedAt)).ToList(),
            latest, production, r.CreatedAt);
    }

    private void CreateProductionJob(CustomProductRequest request)
    {
        if (request.ProductionJob is not null) return;
        var job = new ProductionJob { TenantId = request.TenantId, RequestId = request.Id,
            Number = $"PRD-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ScheduledStart = DateTime.UtcNow.AddDays(1), EstimatedCompletion = DateTime.UtcNow.AddDays(request.EstimatedLeadTimeDays) };
        var stages = new[] { ("materials", "تجهيز الخامات"), ("sample", "إنتاج العينة"), ("printing", "الطباعة"),
            ("finishing", "التشطيب"), ("quality", "فحص الجودة"), ("packing", "التعبئة") };
        for (var i = 0; i < stages.Length; i++) job.Stages.Add(new ProductionStage { TenantId = request.TenantId,
            ProductionJobId = job.Id, Code = stages[i].Item1, NameAr = stages[i].Item2, SortOrder = i + 1 });
        db.ProductionJobs.Add(job); request.Status = CustomRequestStatus.InProduction;
    }

    private async Task<LogoAsset> SaveAssetAsync(Guid requestId, IFormFile file, bool designFile, CancellationToken ct)
    {
        var stored = await StoreFileAsync(TenantId(), requestId, file, 15 * 1024 * 1024, ct);
        return new LogoAsset { TenantId = TenantId(), RequestId = requestId, OriginalName = Path.GetFileName(file.FileName),
            StoredPath = stored.Path, ContentType = stored.ContentType, SizeBytes = file.Length, IsDesignFile = designFile };
    }

    private async Task<(string Path, string ContentType)> StoreFileAsync(Guid tenantId, Guid requestId, IFormFile file, long maxBytes, CancellationToken ct)
    {
        if (file.Length is <= 0 || file.Length > maxBytes) throw ApiException.BadRequest($"حجم الملف يجب ألا يتجاوز {maxBytes / 1024 / 1024} ميجابايت");
        if (!AllowedFiles.TryGetValue(file.ContentType, out var extension)) throw ApiException.BadRequest("صيغة الملف غير مدعومة؛ استخدم PNG أو JPG أو WebP أو SVG أو PDF أو AI");
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "custom", tenantId.ToString("N"), requestId.ToString("N"));
        Directory.CreateDirectory(folder); var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{extension}");
        await using var stream = File.Create(absolute); await file.CopyToAsync(stream, ct);
        return (Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/'), file.ContentType);
    }

    private string SafeStoredPath(string relative)
    {
        var root = Path.GetFullPath(environment.ContentRootPath) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw ApiException.NotFound("الملف غير موجود");
        return path;
    }

    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static CustomChoiceDto Choice(Guid id, string code, string name, string? description, decimal price, string? hex = null) => new(id, code, name, description, price, hex);
    private static string CleanRequired(string? value, int max) => string.IsNullOrWhiteSpace(value) ? throw ApiException.BadRequest("البيان المطلوب غير مكتمل") : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static int Progress(CustomRequestStatus status) => status switch { CustomRequestStatus.Draft => 5, CustomRequestStatus.AwaitingQuote => 15,
        CustomRequestStatus.Quoted => 25, CustomRequestStatus.DesignInProgress => 40, CustomRequestStatus.AwaitingDesignApproval => 55,
        CustomRequestStatus.DesignApproved => 65, CustomRequestStatus.AwaitingSampleApproval => 72,
        CustomRequestStatus.AwaitingCheckout => 67, CustomRequestStatus.AwaitingOrderApproval => 69,
        CustomRequestStatus.InProduction => 75, CustomRequestStatus.QualityCheck => 88,
        CustomRequestStatus.Ready => 95, CustomRequestStatus.Completed => 100, _ => 0 };
}
