using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminPrinting;

public sealed class AdminPrintingService(AppDbContext db)
{
    private static readonly CustomRequestStatus[] ClosedStatuses =
        [CustomRequestStatus.Ready, CustomRequestStatus.Completed, CustomRequestStatus.Rejected, CustomRequestStatus.Cancelled];

    public async Task<PrintingDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        var requests = await BaseQuery().AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        var companies = await db.Companies.AsNoTracking().ToDictionaryAsync(x => x.TenantId, x => x.LegalName, ct);
        var staff = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var rows = requests.Select(x => Row(x, companies, staff)).ToList();
        var designers = await DesignerOptions(requests, ct);
        var templates = await TemplateRows(ct);
        var logos = await db.LogoAssets.AsNoTracking().Include(x => x.Request).Where(x => !x.IsDesignFile)
            .OrderByDescending(x => x.CreatedAt).Select(x => new { Asset = x, x.Request.TenantId }).ToListAsync(ct);
        var activeDesigns = rows.Count(x => x.Status is "DesignInProgress" or "AwaitingDesignApproval");
        var waiting = rows.Count(x => x.Status is "AwaitingDesignApproval" or "AwaitingSampleApproval");
        var production = rows.Count(x => x.Status is "InProduction" or "QualityCheck");
        return new(new(activeDesigns, waiting, production, rows.Count(x => x.IsLate)), rows, designers, templates,
            logos.Select(x => new PrintingLogoLibraryDto(x.Asset.Id, x.Asset.RequestId,
                companies.TryGetValue(x.TenantId, out var company) ? company : "شركة غير معروفة", x.Asset.OriginalName,
                x.Asset.ContentType, x.Asset.SizeBytes, x.Asset.QualityStatus.ToString(), x.Asset.QualityScore,
                x.Asset.CreatedAt, $"/api/admin/custom-products/files/{x.Asset.Id}")).ToList());
    }

    public async Task<PrintingRequestDetailDto> DetailAsync(Guid id, CancellationToken ct = default)
    {
        var request = await DetailQuery().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("طلب الطباعة غير موجود");
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == request.TenantId, ct);
        var users = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff || x.TenantId == request.TenantId)
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var item = request.Items.Single(); var template = request.Template;
        string Choice<T>(IEnumerable<T> values, Guid? choiceId, Func<T, Guid> id, Func<T, string> name) =>
            values.FirstOrDefault(x => id(x) == choiceId) is T value ? name(value) : "—";
        var row = Row(request, new Dictionary<Guid, string> { [request.TenantId] = company?.LegalName ?? "شركة غير معروفة" }, users);
        var production = request.ProductionJob is null ? null : Production(request.ProductionJob, item.Quantity, users);
        return new(row, request.OrderId,
            Choice(template.Options, item.OptionId, x => x.Id, x => x.NameAr),
            Choice(template.PrintMethods, item.PrintMethodId, x => x.Id, x => x.NameAr),
            Choice(template.Materials, item.MaterialId, x => x.Id, x => x.NameAr),
            Choice(template.Colors, item.ColorId, x => x.Id, x => x.NameAr),
            Choice(template.Sizes, item.SizeId, x => x.Id, x => x.NameAr),
            item.PrintWidthCm, item.PrintHeightCm, item.PrintColorCount, request.CustomerNote,
            request.DesignBrief is null ? null : new(request.DesignBrief.Objective, request.DesignBrief.Audience,
                request.DesignBrief.BrandGuidelines, request.DesignBrief.PreferredColors,
                request.DesignBrief.RequiredText, request.DesignBrief.DesiredDate),
            request.LogoAssets.OrderByDescending(x => x.CreatedAt).Select(x => new PrintingAssetDto(x.Id, x.OriginalName,
                x.ContentType, x.SizeBytes, x.IsDesignFile, x.QualityStatus.ToString(), x.QualityScore,
                x.IsVector, x.HasTransparentBackground, x.IsCmykReady, x.HasSufficientResolution,
                x.HasSimpleEffects, Name(users, x.ReviewedBy), x.ReviewedAt, x.ReviewNote,
                $"/api/admin/custom-products/files/{x.Id}")).ToList(),
            request.DesignVersions.OrderByDescending(x => x.VersionNumber).Select(x => new PrintingVersionDto(x.Id,
                x.VersionNumber, x.Title, x.ChangeSummary, Name(users, x.CreatedByDesignerId), x.CreatedAt,
                x.SentToCustomerAt, x.Mockups.Select(m => new PrintingMockupDto(m.Id, m.OriginalName,
                    m.ContentType, m.IsPrimary, $"/api/admin/custom-products/mockups/{m.Id}")).ToList())).ToList(),
            request.Comments.OrderBy(x => x.CreatedAt).Select(x => new PrintingCommentDto(x.Id,
                Name(users, x.UserId) ?? "مستخدم غير معروف", x.Body, x.IsInternal, x.CreatedAt)).ToList(),
            request.Approvals.OrderByDescending(x => x.CreatedAt).Select(x => new PrintingApprovalDto(x.Id,
                x.Version.VersionNumber, x.Decision.ToString(), Name(users, x.UserId) ?? "مسؤول الشركة",
                x.Note, x.CreatedAt)).ToList(), production);
    }

    public async Task AssignDesignerAsync(Guid actor, Guid id, AssignPrintingDesignerDto dto, CancellationToken ct = default)
    {
        var request = await Request(id, ct);
        if (ClosedStatuses.Contains(request.Status) || request.Status is CustomRequestStatus.Draft or CustomRequestStatus.AwaitingQuote or CustomRequestStatus.Quoted)
            throw ApiException.Conflict("حالة الطلب لا تسمح بتعيين مصمم");
        if (dto.DueAt <= DateTime.UtcNow) throw ApiException.BadRequest("موعد التسليم يجب أن يكون في المستقبل");
        var designer = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x =>
            x.Id == dto.DesignerId && x.IsPlatformStaff && x.IsActive &&
            x.Roles.Any(r => r.Role.Code == "graphic_designer" || r.Role.Code == "super_admin"), ct)
            ?? throw ApiException.BadRequest("المصمم المحدد غير صالح");
        request.AssignedDesignerId = designer.Id; request.DesignDueAt = dto.DueAt;
        if (request.Status == CustomRequestStatus.DesignApproved) request.Status = CustomRequestStatus.DesignInProgress;
        if (!string.IsNullOrWhiteSpace(dto.Note)) db.DesignComments.Add(new DesignComment { TenantId = request.TenantId,
            RequestId = id, UserId = actor, Body = Clean(dto.Note, 1000)!, IsInternal = true });
        Audit(actor, request.TenantId, "printing.designer.assign", id, designer.FullName); await db.SaveChangesAsync(ct);
    }

    public async Task ReviewLogoAsync(Guid actor, Guid requestId, Guid assetId, ReviewLogoQualityDto dto, CancellationToken ct = default)
    {
        var asset = await db.LogoAssets.Include(x => x.Request).FirstOrDefaultAsync(x => x.Id == assetId && x.RequestId == requestId, ct)
            ?? throw ApiException.NotFound("ملف الشعار غير موجود");
        if (asset.IsDesignFile) throw ApiException.BadRequest("فحص الجودة مخصص لملفات الشعار");
        if (dto.Score is < 0 or > 100) throw ApiException.BadRequest("درجة الجودة يجب أن تكون بين 0 و100");
        if (dto.Approve && (!dto.HasSufficientResolution || !dto.HasSimpleEffects || dto.Score < 60))
            throw ApiException.BadRequest("لا يمكن اعتماد شعار غير جاهز للطباعة");
        asset.QualityStatus = dto.Approve ? LogoQualityStatus.Approved : LogoQualityStatus.Rejected;
        asset.QualityScore = dto.Score; asset.IsVector = dto.IsVector;
        asset.HasTransparentBackground = dto.HasTransparentBackground; asset.IsCmykReady = dto.IsCmykReady;
        asset.HasSufficientResolution = dto.HasSufficientResolution; asset.HasSimpleEffects = dto.HasSimpleEffects;
        asset.ReviewedBy = actor; asset.ReviewedAt = DateTime.UtcNow; asset.ReviewNote = Clean(dto.Note, 1000);
        Audit(actor, asset.TenantId, "printing.logo.review", requestId, $"{asset.QualityStatus}:{dto.Score}"); await db.SaveChangesAsync(ct);
    }

    public async Task SendDesignAsync(Guid actor, Guid id, SendDesignToCustomerDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(x => x.DesignVersions).Include(x => x.LogoAssets)
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("طلب الطباعة غير موجود");
        var latest = request.DesignVersions.OrderByDescending(x => x.VersionNumber).FirstOrDefault()
            ?? throw ApiException.Conflict("ارفع نسخة تصميم قبل الإرسال");
        if (latest.Id != dto.VersionId) throw ApiException.BadRequest("يجب إرسال أحدث نسخة تصميم");
        if (request.LogoAssets.Any(x => !x.IsDesignFile) && !request.LogoAssets.Any(x => !x.IsDesignFile && x.QualityStatus == LogoQualityStatus.Approved))
            throw ApiException.Conflict("يجب اعتماد ملف شعار واحد على الأقل قبل إرسال التصميم");
        latest.SentToCustomerAt = DateTime.UtcNow; latest.SentByUserId = actor;
        request.DesignSentAt = latest.SentToCustomerAt; request.Status = CustomRequestStatus.AwaitingDesignApproval;
        if (!string.IsNullOrWhiteSpace(dto.Message)) db.DesignComments.Add(new DesignComment { TenantId = request.TenantId,
            RequestId = id, UserId = actor, Body = Clean(dto.Message, 1000)!, IsInternal = false });
        db.Notifications.Add(new AppNotification { TenantId = request.TenantId, UserId = request.UserId,
            Type = "printing.design.ready", Title = "التصميم جاهز للمراجعة", Body = $"النسخة v{latest.VersionNumber} من {request.Number} جاهزة لاعتمادك.",
            EntityType = nameof(CustomProductRequest), EntityId = id });
        Audit(actor, request.TenantId, "printing.design.send", id, $"v{latest.VersionNumber}"); await db.SaveChangesAsync(ct);
    }

    public async Task AddInternalCommentAsync(Guid actor, Guid id, AddInternalPrintingCommentDto dto, CancellationToken ct = default)
    {
        var request = await Request(id, ct); var body = Clean(dto.Body, 1500);
        if (body is null) throw ApiException.BadRequest("اكتب الملاحظة الداخلية");
        db.DesignComments.Add(new DesignComment { TenantId = request.TenantId, RequestId = id, UserId = actor, Body = body, IsInternal = true });
        await db.SaveChangesAsync(ct);
    }

    public async Task StartProductionAsync(Guid actor, Guid id, StartPrintingProductionDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(x => x.Approvals).Include(x => x.ProductionJob!).ThenInclude(x => x.Stages)
            .Include(x => x.ProductionJob!).ThenInclude(x => x.Samples).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("طلب الطباعة غير موجود");
        var job = request.ProductionJob ?? throw ApiException.Conflict("يجب ربط الطلب بأمر بيع وإنشاء أمر الإنتاج أولًا");
        if (request.OrderId is null || !request.Approvals.Any(x => x.Decision == DesignApprovalDecision.Approved))
            throw ApiException.Conflict("لا يبدأ الإنتاج قبل الطلب التجاري واعتماد التصميم");
        var latestSample = job.Samples.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        if (latestSample is null || latestSample.Decision != SampleApprovalDecision.Approved)
            throw ApiException.Conflict("يجب اعتماد عينة الإنتاج أولًا");
        var start = dto.ScheduledStart ?? DateTime.UtcNow; var end = dto.EstimatedCompletion ?? start.AddDays(Math.Max(1, request.EstimatedLeadTimeDays));
        if (end <= start) throw ApiException.BadRequest("موعد اكتمال الإنتاج غير صالح");
        job.ScheduledStart = start; job.EstimatedCompletion = end; job.ActualStart ??= DateTime.UtcNow;
        var first = job.Stages.OrderBy(x => x.SortOrder).First(); first.Status = ProductionStageStatus.InProgress; first.StartedAt ??= DateTime.UtcNow;
        request.Status = CustomRequestStatus.InProduction;
        Audit(actor, request.TenantId, "printing.production.start", id, job.Number); await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStageAsync(Guid actor, Guid requestId, Guid stageId, UpdatePrintingStageDto dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ProductionStageStatus>(dto.Status, true, out var status)) throw ApiException.BadRequest("حالة المرحلة غير صالحة");
        var request = await db.CustomProductRequests.Include(x => x.Items).Include(x => x.ProductionJob!).ThenInclude(x => x.Stages)
            .Include(x => x.ProductionJob!).ThenInclude(x => x.QualityChecks).FirstOrDefaultAsync(x => x.Id == requestId, ct)
            ?? throw ApiException.NotFound("طلب الطباعة غير موجود");
        var job = request.ProductionJob ?? throw ApiException.Conflict("أمر الإنتاج غير موجود");
        var stage = job.Stages.FirstOrDefault(x => x.Id == stageId) ?? throw ApiException.NotFound("مرحلة الإنتاج غير موجودة");
        var target = request.Items.Single().Quantity;
        if (dto.ProducedQuantity is < 0 || dto.ProducedQuantity > target) throw ApiException.BadRequest("الكمية المنجزة غير صالحة");
        if (status == ProductionStageStatus.Completed && job.Stages.Any(x => x.SortOrder < stage.SortOrder && x.Status != ProductionStageStatus.Completed))
            throw ApiException.Conflict("يجب إكمال مراحل الإنتاج السابقة بالترتيب");
        if (stage.Code == "quality" && status == ProductionStageStatus.Completed && !job.QualityChecks.Any(x => x.Passed))
            throw ApiException.Conflict("يجب تسجيل فحص جودة ناجح");
        if (stage.Code == "packing" && status == ProductionStageStatus.Completed && (job.PackageCount <= 0 || job.UnitsPerPackage <= 0))
            throw ApiException.Conflict("سجل بيانات التغليف قبل إكمال المرحلة");
        stage.Status = status; stage.Note = Clean(dto.Note, 1000); job.ProducedQuantity = Math.Max(job.ProducedQuantity, dto.ProducedQuantity);
        if (status == ProductionStageStatus.InProgress) stage.StartedAt ??= DateTime.UtcNow;
        if (status == ProductionStageStatus.Completed) { stage.StartedAt ??= DateTime.UtcNow; stage.CompletedAt = DateTime.UtcNow; }
        request.Status = stage.Code == "quality" && status == ProductionStageStatus.InProgress ? CustomRequestStatus.QualityCheck : CustomRequestStatus.InProduction;
        if (job.Stages.All(x => x.Id == stage.Id ? status == ProductionStageStatus.Completed : x.Status == ProductionStageStatus.Completed)) job.ActualCompletion = DateTime.UtcNow;
        Audit(actor, request.TenantId, "printing.production.stage", requestId, $"{stage.Code}:{status}"); await db.SaveChangesAsync(ct);
    }

    public async Task SavePackagingAsync(Guid actor, Guid id, SavePackagingDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(x => x.ProductionJob).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("طلب الطباعة غير موجود");
        var job = request.ProductionJob ?? throw ApiException.Conflict("أمر الإنتاج غير موجود");
        if (string.IsNullOrWhiteSpace(dto.PackagingType) || dto.UnitsPerPackage <= 0 || dto.PackageCount <= 0)
            throw ApiException.BadRequest("بيانات التغليف غير صالحة");
        job.PackagingType = Clean(dto.PackagingType, 200); job.UnitsPerPackage = dto.UnitsPerPackage; job.PackageCount = dto.PackageCount;
        Audit(actor, request.TenantId, "printing.packaging", id, $"{dto.PackageCount}x{dto.UnitsPerPackage}"); await db.SaveChangesAsync(ct);
    }

    public async Task MarkReadyAsync(Guid actor, Guid id, MarkPrintingReadyDto dto, CancellationToken ct = default)
    {
        var request = await db.CustomProductRequests.Include(x => x.Items).Include(x => x.ProductionJob!).ThenInclude(x => x.Stages)
            .Include(x => x.ProductionJob!).ThenInclude(x => x.QualityChecks).FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("طلب الطباعة غير موجود");
        var job = request.ProductionJob ?? throw ApiException.Conflict("أمر الإنتاج غير موجود"); var target = request.Items.Single().Quantity;
        if (job.Stages.Any(x => x.Status != ProductionStageStatus.Completed) || !job.QualityChecks.Any(x => x.Passed) ||
            job.ProducedQuantity < target || job.PackageCount <= 0) throw ApiException.Conflict("لم تكتمل مراحل الإنتاج والجودة والتغليف والكمية المطلوبة");
        request.Status = CustomRequestStatus.Ready; request.ReadyAt = DateTime.UtcNow; job.ActualCompletion ??= request.ReadyAt;
        job.DispatchReference = Clean(dto.DispatchReference, 100);
        db.Notifications.Add(new AppNotification { TenantId = request.TenantId, UserId = request.UserId,
            Type = "printing.ready", Title = "طلب الطباعة جاهز للشحن", Body = $"اكتمل إنتاج وتغليف الطلب {request.Number}.",
            EntityType = nameof(CustomProductRequest), EntityId = id });
        Audit(actor, request.TenantId, "printing.ready", id, job.DispatchReference); await db.SaveChangesAsync(ct);
    }

    public async Task UpdateTemplateAsync(Guid actor, Guid id, SavePrintingTemplateDto dto, CancellationToken ct = default)
    {
        var template = await db.CustomProductTemplates.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw ApiException.NotFound("قالب الطباعة غير موجود");
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.SetupFee < 0 || dto.MinQuantity < 1 || dto.LeadTimeDays is < 1 or > 180)
            throw ApiException.BadRequest("بيانات قالب الطباعة غير صالحة");
        template.NameAr = Clean(dto.Name, 200)!; template.DescriptionAr = Clean(dto.Description, 1000);
        template.SetupFee = dto.SetupFee; template.MinQuantity = dto.MinQuantity; template.LeadTimeDays = dto.LeadTimeDays;
        template.IsActive = dto.IsActive; template.UpdatedBy = actor;
        db.AuditLogs.Add(new AuditLog { UserId = actor, Action = "printing.template.update", EntityType = nameof(CustomProductTemplate), EntityId = id.ToString(), DataJson = template.NameAr });
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<CustomProductRequest> BaseQuery() => db.CustomProductRequests.AsSplitQuery()
        .Include(x => x.Template).ThenInclude(x => x.Product).Include(x => x.Items)
        .Include(x => x.DesignVersions).Include(x => x.ProductionJob!).ThenInclude(x => x.Stages);
    private IQueryable<CustomProductRequest> DetailQuery() => BaseQuery()
        .Include(x => x.Template).ThenInclude(x => x.Options).Include(x => x.Template).ThenInclude(x => x.PrintMethods)
        .Include(x => x.Template).ThenInclude(x => x.Materials).Include(x => x.Template).ThenInclude(x => x.Colors)
        .Include(x => x.Template).ThenInclude(x => x.Sizes).Include(x => x.LogoAssets).Include(x => x.DesignBrief)
        .Include(x => x.DesignVersions).ThenInclude(x => x.Mockups).Include(x => x.Comments)
        .Include(x => x.Approvals).ThenInclude(x => x.Version).Include(x => x.ProductionJob!).ThenInclude(x => x.Samples)
        .Include(x => x.ProductionJob!).ThenInclude(x => x.QualityChecks);
    private async Task<CustomProductRequest> Request(Guid id, CancellationToken ct) => await db.CustomProductRequests.FirstOrDefaultAsync(x => x.Id == id, ct)
        ?? throw ApiException.NotFound("طلب الطباعة غير موجود");

    private static PrintingRequestRowDto Row(CustomProductRequest r, IReadOnlyDictionary<Guid, string> companies, IReadOnlyDictionary<Guid, string> staff)
    {
        var target = r.Items.SingleOrDefault()?.Quantity ?? 0; var now = DateTime.UtcNow;
        var due = r.DesignDueAt ?? r.ProductionJob?.EstimatedCompletion;
        var late = due < now && !ClosedStatuses.Contains(r.Status);
        var stages = r.ProductionJob?.Stages.ToList() ?? [];
        var progress = stages.Count > 0 ? (int)Math.Round(stages.Count(x => x.Status == ProductionStageStatus.Completed) * 100m / stages.Count)
            : r.Status switch { CustomRequestStatus.DesignInProgress => 30, CustomRequestStatus.AwaitingDesignApproval => 45,
                CustomRequestStatus.DesignApproved => 55, CustomRequestStatus.AwaitingSampleApproval => 65,
                CustomRequestStatus.Ready => 100, CustomRequestStatus.Completed => 100, _ => 10 };
        return new(r.Id, r.Number, companies.TryGetValue(r.TenantId, out var company) ? company : "شركة غير معروفة",
            r.Template.Product.NameAr, target, r.Status.ToString(), r.AssignedDesignerId,
            Name(staff, r.AssignedDesignerId), due, r.DesignVersions.Count, progress, late, r.CreatedAt);
    }

    private static PrintingProductionDto Production(ProductionJob x, int target, IReadOnlyDictionary<Guid, string> users) =>
        new(x.Id, x.Number, x.ScheduledStart, x.EstimatedCompletion, x.ActualStart, x.ActualCompletion,
            x.ProducedQuantity, target, x.PackagingType, x.UnitsPerPackage, x.PackageCount, x.DispatchReference,
            x.Stages.OrderBy(s => s.SortOrder).Select(s => new PrintingStageDto(s.Id, s.Code, s.NameAr,
                s.Status.ToString(), s.SortOrder, s.StartedAt, s.CompletedAt, s.Note)).ToList(),
            x.Samples.OrderByDescending(s => s.VersionNumber).Select(s => new PrintingSampleDto(s.Id, s.VersionNumber,
                s.OriginalName, s.Decision.ToString(), s.Note, s.DecisionNote, s.CreatedAt,
                $"/api/admin/custom-products/samples/{s.Id}")).ToList(),
            x.QualityChecks.OrderByDescending(q => q.CheckedAt).Select(q => new PrintingQualityDto(q.Id,
                q.CheckNameAr, q.Passed, Name(users, q.CheckedBy), q.CheckedAt, q.Note)).ToList());

    private async Task<IReadOnlyList<PrintingDesignerDto>> DesignerOptions(IReadOnlyList<CustomProductRequest> requests, CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().Include(x => x.Roles).ThenInclude(x => x.Role)
            .Where(x => x.IsPlatformStaff && x.IsActive && x.Roles.Any(r => r.Role.Code == "graphic_designer" || r.Role.Code == "super_admin"))
            .OrderBy(x => x.FullName).ToListAsync(ct);
        return users.Select(x => new PrintingDesignerDto(x.Id, x.FullName,
            x.Roles.Any(r => r.Role.Code == "graphic_designer") ? "تصميم ومطبوعات" : "إدارة التصميم",
            requests.Count(r => r.AssignedDesignerId == x.Id && !ClosedStatuses.Contains(r.Status)))).ToList();
    }

    private async Task<IReadOnlyList<PrintingTemplateDto>> TemplateRows(CancellationToken ct) => await db.CustomProductTemplates
        .AsNoTracking().AsSplitQuery().Include(x => x.Product).Include(x => x.Options).Include(x => x.PrintMethods).Include(x => x.Materials)
        .Include(x => x.Colors).Include(x => x.Sizes).OrderBy(x => x.NameAr)
        .Select(x => new PrintingTemplateDto(x.Id, x.ProductId, x.Product.Sku, x.NameAr, x.DescriptionAr,
            x.Product.BasePrice, x.SetupFee, x.MinQuantity, x.LeadTimeDays, x.IsActive, x.Options.Count,
            x.PrintMethods.Count, x.Materials.Count, x.Colors.Count, x.Sizes.Count)).ToListAsync(ct);

    private void Audit(Guid actor, Guid tenant, string action, Guid id, string? data) => db.AuditLogs.Add(new AuditLog
        { UserId = actor, TenantId = tenant, Action = action, EntityType = nameof(CustomProductRequest), EntityId = id.ToString(), DataJson = data });
    private static string? Name(IReadOnlyDictionary<Guid, string> users, Guid? id) => id is Guid value && users.TryGetValue(value, out var name) ? name : null;
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
