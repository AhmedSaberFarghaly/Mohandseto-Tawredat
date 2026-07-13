using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Approvals;

public sealed class ApprovalService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment environment,
    CustomizationService customization)
{
    private static readonly Dictionary<string, string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    { ["application/pdf"] = ".pdf", ["image/png"] = ".png", ["image/jpeg"] = ".jpg" };

    public async Task CreateForOrderAsync(Order order, bool budgetConflict, Guid requesterId, CancellationToken ct = default)
    {
        var policy = await db.ApprovalPolicies.Include(p => p.Levels).ThenInclude(l => l.Assignments)
            .Where(p => p.IsActive && p.MinimumAmount <= order.Total).OrderByDescending(p => p.MinimumAmount).FirstOrDefaultAsync(ct)
            ?? throw ApiException.Conflict("لا توجد سياسة موافقات مهيأة للشركة");
        var levels = policy.Levels.OrderBy(l => l.Sequence).ToList();
        if (levels.Count == 0 || levels.Any(l => l.Assignments.Count == 0))
            throw ApiException.Conflict("سلسلة الموافقات غير مكتملة");
        var request = new ApprovalRequest { TenantId = TenantId(), Number = $"APR-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            OrderId = order.Id, Order = order, PolicyId = policy.Id, RequestedByUserId = requesterId, HasBudgetConflict = budgetConflict,
            CurrentLevelSequence = levels[0].Sequence, DueAt = DateTime.UtcNow.AddHours(levels[0].SlaHours) };
        foreach (var level in levels)
            request.Steps.Add(new ApprovalStep { TenantId = TenantId(), LevelId = level.Id, Sequence = level.Sequence,
                NameAr = level.NameAr, ApproverUserId = level.Assignments.OrderBy(a => a.CreatedAt).First().UserId,
                AuthorityLimit = level.AuthorityLimit, Status = level.Sequence == levels[0].Sequence ? ApprovalStepStatus.Current : ApprovalStepStatus.Waiting });
        request.Actions.Add(new ApprovalAction { TenantId = TenantId(), ActorUserId = requesterId,
            Type = ApprovalActionType.Submitted, LevelSequence = levels[0].Sequence, Comment = "إرسال الطلب للموافقة" });
        db.ApprovalRequests.Add(request);
        Notify(request.Steps.First().ApproverUserId, "approval.created", "طلب موافقة جديد",
            $"الطلب {order.Number} بقيمة {order.Total:0.##} ج.م ينتظر قرارك", request.Id);
    }

    public async Task<IReadOnlyList<ApprovalListItemDto>> InboxAsync(Guid userId, string? status, CancellationToken ct = default)
    {
        await EscalateOverdueAsync(ct);
        var query = db.ApprovalRequests.AsNoTracking().Include(r => r.Order).Include(r => r.Steps)
            .Where(r => r.RequestedByUserId == userId || r.Steps.Any(s => s.ApproverUserId == userId));
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApprovalRequestStatus>(status, true, out var parsed)) query = query.Where(r => r.Status == parsed);
        var data = await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
        var users = await UserNamesAsync(data.Select(r => r.RequestedByUserId), ct);
        return data.Select(r => { var current = r.Steps.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Status == ApprovalStepStatus.Current);
            return new ApprovalListItemDto(r.Id, r.Number, r.Order.Number, r.Order.Total, users.GetValueOrDefault(r.RequestedByUserId, "مستخدم"),
                current?.NameAr ?? "مكتملة", r.Status.ToString(), r.HasBudgetConflict, r.DueAt, r.Status == ApprovalRequestStatus.Pending && r.DueAt < DateTime.UtcNow); }).ToList();
    }

    public async Task<ApprovalDetailDto> DetailAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var request = await LoadAsync(id, ct);
        if (request.RequestedByUserId != userId && !request.Steps.Any(s => s.ApproverUserId == userId)) throw ApiException.Forbidden("لا يمكنك عرض طلب الموافقة");
        var userIds = request.Steps.Select(s => s.ApproverUserId).Append(request.RequestedByUserId)
            .Concat(request.Actions.Select(a => a.ActorUserId));
        var users = await UserNamesAsync(userIds, ct);
        var center = request.Order.CostCenterId is null ? null : await db.CostCenters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Order.CostCenterId, ct);
        var current = request.Steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Current);
        var canAct = request.Status == ApprovalRequestStatus.Pending && current?.ApproverUserId == userId;
        return new(request.Id, request.Number, request.OrderId, request.Order.Number, request.Order.Total,
            users.GetValueOrDefault(request.RequestedByUserId, "مستخدم"), request.Order.RequestingDepartment ?? "—", request.Order.CostCenterName,
            center is null ? null : center.BudgetAmount - center.UsedAmount - center.ReservedAmount, request.HasBudgetConflict,
            request.Status.ToString(), request.CurrentLevelSequence, request.DueAt,
            request.Steps.OrderBy(s => s.Sequence).Select(s => new ApprovalStepDto(s.Id, s.Sequence, s.NameAr,
                users.GetValueOrDefault(s.ApproverUserId, "مسؤول موافقة"), s.Status.ToString(), s.AuthorityLimit, s.DecidedAt, s.ApproverUserId == userId)).ToList(),
            request.Actions.OrderByDescending(a => a.CreatedAt).Select(a => new ApprovalActionDto(a.Id,
                users.GetValueOrDefault(a.ActorUserId, "مستخدم"), a.Type.ToString(), a.LevelSequence, a.Comment, a.CreatedAt)).ToList(),
            request.Attachments.OrderByDescending(a => a.CreatedAt).Select(Attachment).ToList(), canAct,
            canAct && current?.AuthorityLimit is { } limit && request.Order.Total > limit);
    }

    public Task<ApprovalDetailDto> ApproveAsync(Guid userId, Guid id, string? comment, CancellationToken ct = default) =>
        DecideAsync(userId, id, ApprovalActionType.Approved, comment, ct);
    public Task<ApprovalDetailDto> RejectAsync(Guid userId, Guid id, string? comment, CancellationToken ct = default) =>
        DecideAsync(userId, id, ApprovalActionType.Rejected, RequireComment(comment), ct);
    public Task<ApprovalDetailDto> RequestChangesAsync(Guid userId, Guid id, string? comment, CancellationToken ct = default) =>
        DecideAsync(userId, id, ApprovalActionType.ChangesRequested, RequireComment(comment), ct);

    private async Task<ApprovalDetailDto> DecideAsync(Guid userId, Guid id, ApprovalActionType type, string? comment, CancellationToken ct)
    {
        var request = await LoadAsync(id, ct); var step = CurrentStep(request, userId);
        step.DecidedAt = DateTime.UtcNow; step.Status = type switch { ApprovalActionType.Approved => ApprovalStepStatus.Approved,
            ApprovalActionType.Rejected => ApprovalStepStatus.Rejected, _ => ApprovalStepStatus.ChangesRequested };
        request.Actions.Add(Action(request, userId, type, comment));
        if (type == ApprovalActionType.Rejected)
        {
            request.Status = ApprovalRequestStatus.Rejected; request.CompletedAt = DateTime.UtcNow;
            request.Order.Status = OrderStatus.Cancelled; await ReleaseBudgetAsync(request.Order, ct);
            await customization.ResolveOrderApprovalAsync(request.OrderId, false, ct);
            Notify(request.RequestedByUserId, "approval.rejected", "تم رفض الطلب", $"رُفض الطلب {request.Order.Number}", request.Id);
        }
        else if (type == ApprovalActionType.ChangesRequested)
        {
            request.Status = ApprovalRequestStatus.ChangesRequested;
            Notify(request.RequestedByUserId, "approval.changes", "مطلوب تعديل الطلب", comment!, request.Id);
        }
        else
        {
            var next = request.Steps.OrderBy(s => s.Sequence).FirstOrDefault(s => s.Status == ApprovalStepStatus.Waiting);
            if (next is null)
            {
                request.Status = ApprovalRequestStatus.Approved; request.CompletedAt = DateTime.UtcNow;
                request.Order.Status = OrderStatus.Confirmed; await CommitBudgetAsync(request.Order, ct);
                await customization.ResolveOrderApprovalAsync(request.OrderId, true, ct);
                Notify(request.RequestedByUserId, "approval.approved", "تم اعتماد الطلب", $"اكتملت موافقات الطلب {request.Order.Number}", request.Id);
            }
            else
            {
                next.Status = ApprovalStepStatus.Current; request.CurrentLevelSequence = next.Sequence;
                var level = await db.ApprovalLevels.AsNoTracking().FirstAsync(l => l.Id == next.LevelId, ct);
                request.DueAt = DateTime.UtcNow.AddHours(level.SlaHours);
                Notify(next.ApproverUserId, "approval.created", "طلب موافقة جديد", $"الطلب {request.Order.Number} وصل إلى مرحلتك", request.Id);
            }
        }
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<ApprovalDetailDto> DelegateAsync(Guid userId, Guid id, ApprovalDelegateDto dto, CancellationToken ct = default)
    {
        var request = await LoadAsync(id, ct); var step = CurrentStep(request, userId);
        if (!await db.Users.AnyAsync(u => u.Id == dto.UserId && u.TenantId == TenantId() && u.IsActive, ct)) throw ApiException.BadRequest("المستخدم المفوض إليه غير صالح");
        step.DelegatedFromUserId = userId; step.ApproverUserId = dto.UserId;
        var action = new ApprovalAction { TenantId = TenantId(), RequestId = request.Id,
            ActorUserId = userId, Type = ApprovalActionType.Delegated, LevelSequence = step.Sequence,
            Comment = Clean(dto.Comment, 1000), DelegateToUserId = dto.UserId };
        db.ApprovalActions.Add(action); request.Actions.Add(action);
        Notify(dto.UserId, "approval.delegated", "تم تفويض موافقة إليك", $"الطلب {request.Order.Number}", request.Id);
        await db.SaveChangesAsync(ct); return await DetailAsync(dto.UserId, id, ct);
    }

    public async Task<ApprovalDetailDto> CommentAsync(Guid userId, Guid id, string comment, CancellationToken ct = default)
    {
        var request = await LoadAsync(id, ct);
        if (request.RequestedByUserId != userId && !request.Steps.Any(s => s.ApproverUserId == userId)) throw ApiException.Forbidden("لا يمكنك التعليق");
        request.Actions.Add(Action(request, userId, ApprovalActionType.Commented, RequireComment(comment)));
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<ApprovalDetailDto> ResubmitAsync(Guid userId, Guid id, string? comment, CancellationToken ct = default)
    {
        var request = await LoadAsync(id, ct);
        if (request.RequestedByUserId != userId || request.Status != ApprovalRequestStatus.ChangesRequested) throw ApiException.Conflict("الطلب غير متاح لإعادة الإرسال");
        var step = request.Steps.First(s => s.Status == ApprovalStepStatus.ChangesRequested);
        step.Status = ApprovalStepStatus.Current; step.DecidedAt = null; request.Status = ApprovalRequestStatus.Pending;
        request.CurrentLevelSequence = step.Sequence; request.DueAt = DateTime.UtcNow.AddHours(12);
        request.Actions.Add(Action(request, userId, ApprovalActionType.Resubmitted, Clean(comment, 1000)));
        await db.SaveChangesAsync(ct); return await DetailAsync(userId, id, ct);
    }

    public async Task<ApprovalAttachmentDto> UploadAsync(Guid userId, Guid id, IFormFile file, CancellationToken ct = default)
    {
        var request = await LoadAsync(id, ct);
        if (request.RequestedByUserId != userId && !request.Steps.Any(s => s.ApproverUserId == userId)) throw ApiException.Forbidden("لا يمكنك إضافة مرفق");
        if (file.Length is <= 0 or > 10 * 1024 * 1024 || !AllowedFiles.TryGetValue(file.ContentType, out var ext)) throw ApiException.BadRequest("المرفق يجب أن يكون PDF أو PNG أو JPG وبحجم أقصى 10MB");
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "approvals", TenantId().ToString("N"), request.Id.ToString("N"));
        Directory.CreateDirectory(folder); var absolute = Path.Combine(folder, $"{Guid.NewGuid():N}{ext}");
        await using (var stream = File.Create(absolute)) await file.CopyToAsync(stream, ct);
        var attachment = new ApprovalAttachment { TenantId = TenantId(), RequestId = request.Id, UploadedByUserId = userId,
            OriginalName = Path.GetFileName(file.FileName), StoredPath = Path.GetRelativePath(environment.ContentRootPath, absolute).Replace('\\', '/'),
            ContentType = file.ContentType, SizeBytes = file.Length };
        db.ApprovalAttachments.Add(attachment); await db.SaveChangesAsync(ct); return Attachment(attachment);
    }

    public async Task<(string Path, string Type, string Name)> FileAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var file = await db.ApprovalAttachments.AsNoTracking().Include(a => a.Request).ThenInclude(r => r.Steps)
            .FirstOrDefaultAsync(a => a.Id == id && (a.Request.RequestedByUserId == userId || a.Request.Steps.Any(s => s.ApproverUserId == userId)), ct)
            ?? throw ApiException.NotFound("المرفق غير موجود");
        return (SafePath(file.StoredPath), file.ContentType, file.OriginalName);
    }

    public Task<List<ApprovalUserDto>> UsersAsync(CancellationToken ct = default) => db.Users.AsNoTracking()
        .Where(u => u.TenantId == TenantId() && u.IsActive).OrderBy(u => u.FullName)
        .Select(u => new ApprovalUserDto(u.Id, u.FullName, u.Phone)).ToListAsync(ct);

    private async Task<ApprovalRequest> LoadAsync(Guid id, CancellationToken ct) => await db.ApprovalRequests
        .Include(r => r.Order).Include(r => r.Steps).Include(r => r.Actions).Include(r => r.Attachments)
        .FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("طلب الموافقة غير موجود");
    private static ApprovalStep CurrentStep(ApprovalRequest request, Guid userId) =>
        request.Status == ApprovalRequestStatus.Pending && request.Steps.FirstOrDefault(s => s.Status == ApprovalStepStatus.Current && s.ApproverUserId == userId) is { } step
            ? step : throw ApiException.Forbidden("الطلب ليس بانتظار موافقتك");
    private ApprovalAction Action(ApprovalRequest request, Guid actor, ApprovalActionType type, string? comment)
    {
        var action = new ApprovalAction { TenantId = TenantId(), RequestId = request.Id,
            ActorUserId = actor, Type = type, LevelSequence = request.CurrentLevelSequence, Comment = Clean(comment, 1000) };
        db.ApprovalActions.Add(action); return action;
    }
    private async Task CommitBudgetAsync(Order order, CancellationToken ct) { if (order.CostCenterId is not { } id) return;
        var center = await db.CostCenters.FirstAsync(c => c.Id == id, ct); center.ReservedAmount = Math.Max(0, center.ReservedAmount - order.Total); center.UsedAmount += order.Total; }
    private async Task ReleaseBudgetAsync(Order order, CancellationToken ct) { if (order.CostCenterId is not { } id) return;
        var center = await db.CostCenters.FirstAsync(c => c.Id == id, ct); center.ReservedAmount = Math.Max(0, center.ReservedAmount - order.Total); }
    private async Task EscalateOverdueAsync(CancellationToken ct) { var due = await db.ApprovalRequests.Include(r => r.Steps)
        .Where(r => r.Status == ApprovalRequestStatus.Pending && r.DueAt < DateTime.UtcNow).ToListAsync(ct); foreach (var r in due) {
            r.Actions.Add(Action(r, r.RequestedByUserId, ApprovalActionType.Escalated, "تجاوزت مهلة الموافقة")); r.DueAt = DateTime.UtcNow.AddHours(12); } if (due.Count > 0) await db.SaveChangesAsync(ct); }
    private void Notify(Guid userId, string type, string title, string body, Guid entityId) => db.Notifications.Add(new AppNotification
    { TenantId = TenantId(), UserId = userId, Type = type, Title = title, Body = body, EntityType = nameof(ApprovalRequest), EntityId = entityId });
    private async Task<Dictionary<Guid, string>> UserNamesAsync(IEnumerable<Guid> ids, CancellationToken ct) { var list = ids.Distinct().ToList();
        return await db.Users.AsNoTracking().Where(u => list.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct); }
    private string SafePath(string relative) { var root = Path.GetFullPath(environment.ContentRootPath) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative)); if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) throw ApiException.NotFound("المرفق غير موجود"); return path; }
    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("الحساب غير مرتبط بشركة");
    private static string RequireComment(string? value) => string.IsNullOrWhiteSpace(value) ? throw ApiException.BadRequest("سبب القرار مطلوب") : value.Trim();
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static ApprovalAttachmentDto Attachment(ApprovalAttachment a) => new(a.Id, a.OriginalName, a.SizeBytes, $"/api/approvals/attachments/{a.Id}");
}
