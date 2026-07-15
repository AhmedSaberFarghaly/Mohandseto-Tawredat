using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Auth;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Account;

public sealed class AccountService(AppDbContext db, ITenantProvider tenantProvider, IWebHostEnvironment env, AdminSystemSettingsService? systemSettings = null)
{
    private static readonly HashSet<string> ClientSystemRoles =
    ["company_owner", "purchasing_officer", "company_admin", "finance_manager", "warehouse_officer", "department_manager", "approver", "billing_officer", "requester"];
    private static readonly string[] LogoTypes = ["image/png", "image/jpeg", "image/svg+xml"];

    public async Task<AccountOverviewDto> OverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await ProfileAsync(userId, ct);
        var company = await CompanyAsync(ct);
        var tenantId = TenantId();
        var users = await db.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);
        var branches = await db.CompanyBranches.CountAsync(ct);
        var invites = await db.CompanyInvites.CountAsync(i => i.Status == CompanyInviteStatus.Pending && i.ExpiresAt > DateTime.UtcNow, ct);
        var entity = await CurrentCompany(ct);
        var ends = await db.CompanyContracts.Where(c => c.Status == CompanyContractStatus.Active || c.Status == CompanyContractStatus.Expiring).MaxAsync(c => (DateTime?)c.EndsAt, ct);
        return new(profile, company, users, branches, invites, entity.CreditLimit, entity.CreditUsed, ends);
    }

    public async Task<ProfileDto> ProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var tenantId = TenantId();
        var user = await db.Users.Include(u => u.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, ct)
            ?? throw ApiException.NotFound("المستخدم غير موجود");
        return Profile(user);
    }

    public async Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
    {
        var user = await TenantUser(userId, true, ct);
        user.FullName = Required(dto.FullName, 160, "الاسم مطلوب");
        var email = Clean(dto.Email, 200)?.ToLowerInvariant();
        if (email != user.Email && email is not null && await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != user.Id, ct)) throw ApiException.Conflict("البريد الإلكتروني مستخدم بالفعل");
        if (dto.DefaultBranchId is { } bid && !await db.CompanyBranches.AnyAsync(b => b.Id == bid, ct)) throw ApiException.BadRequest("الفرع غير صالح");
        user.Email = email; user.PreferredLanguage = dto.Language is "ar" or "en" ? dto.Language : "ar";
        user.JobTitle = Clean(dto.JobTitle, 120); user.Department = Clean(dto.Department, 120); user.DefaultBranchId = dto.DefaultBranchId;
        Audit(userId, "account.profile_updated", nameof(User), user.Id); await db.SaveChangesAsync(ct); return Profile(user);
    }

    public async Task<CompanyDto> CompanyAsync(CancellationToken ct = default)
    {
        var company = await CurrentCompany(ct); var status = await db.Tenants.Where(t => t.Id == TenantId()).Select(t => t.Status.ToString()).SingleAsync(ct);
        return Company(company, status);
    }

    public async Task<CompanyDto> UpdateCompanyAsync(Guid actorId, UpdateCompanyDto dto, CancellationToken ct = default)
    {
        var c = await CurrentCompany(ct); c.LegalName = Required(dto.LegalName, 240, "اسم الشركة مطلوب"); c.LegalNameEn = Clean(dto.LegalNameEn, 240);
        c.Phone = Required(dto.Phone, 30, "رقم الهاتف مطلوب"); c.Email = Clean(dto.Email, 200)?.ToLowerInvariant(); c.Governorate = Clean(dto.Governorate, 100);
        c.City = Clean(dto.City, 100); c.AddressLine = Clean(dto.AddressLine, 500); c.Industry = Clean(dto.Industry, 160); c.EmployeeCountRange = Math.Max(0, dto.EmployeeCountRange);
        var tenant = await db.Tenants.SingleAsync(t => t.Id == TenantId(), ct); tenant.Name = c.LegalName;
        Audit(actorId, "company.profile_updated", nameof(Company), c.Id); await db.SaveChangesAsync(ct); return Company(c, tenant.Status.ToString());
    }

    public async Task<List<BranchDto>> BranchesAsync(CancellationToken ct = default) => await db.CompanyBranches.AsNoTracking().OrderByDescending(b => b.IsMain).ThenBy(b => b.Name).Select(b => Branch(b)).ToListAsync(ct);

    public async Task<BranchDto> CreateBranchAsync(Guid actorId, UpsertBranchDto dto, CancellationToken ct = default)
    {
        var company = await CurrentCompany(ct); if (dto.IsMain) foreach (var old in await db.CompanyBranches.Where(b => b.IsMain).ToListAsync(ct)) old.IsMain = false;
        var branch = new CompanyBranch { TenantId = TenantId(), CompanyId = company.Id }; Apply(branch, dto); db.CompanyBranches.Add(branch);
        Audit(actorId, "company.branch_created", nameof(CompanyBranch), branch.Id, new { branch.Name }); await db.SaveChangesAsync(ct); return Branch(branch);
    }

    public async Task<BranchDto> UpdateBranchAsync(Guid actorId, Guid id, UpsertBranchDto dto, CancellationToken ct = default)
    {
        var branch = await db.CompanyBranches.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw ApiException.NotFound("الفرع غير موجود");
        if (dto.IsMain) foreach (var old in await db.CompanyBranches.Where(b => b.IsMain && b.Id != id).ToListAsync(ct)) old.IsMain = false;
        Apply(branch, dto); Audit(actorId, "company.branch_updated", nameof(CompanyBranch), id); await db.SaveChangesAsync(ct); return Branch(branch);
    }

    public async Task DeleteBranchAsync(Guid actorId, Guid id, CancellationToken ct = default)
    {
        var branch = await db.CompanyBranches.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw ApiException.NotFound("الفرع غير موجود");
        if (branch.IsMain) throw ApiException.Conflict("لا يمكن حذف الفرع الرئيسي");
        if (await db.Users.AnyAsync(u => u.TenantId == TenantId() && u.DefaultBranchId == id, ct) || await db.Orders.AnyAsync(o => o.BranchId == id, ct)) throw ApiException.Conflict("الفرع مرتبط بمستخدمين أو طلبات ولا يمكن حذفه");
        db.CompanyBranches.Remove(branch); Audit(actorId, "company.branch_deleted", nameof(CompanyBranch), id); await db.SaveChangesAsync(ct);
    }

    public async Task<List<CompanyDocumentDto>> DocumentsAsync(CancellationToken ct = default) => await db.CompanyDocuments.AsNoTracking().OrderBy(d => d.Type).Select(d => new CompanyDocumentDto(d.Id, d.Type.ToString(), d.FileName, d.SizeBytes, d.ReviewStatus.ToString(), d.RejectionReason, d.CreatedAt)).ToListAsync(ct);

    public async Task<List<CompanyUserDto>> UsersAsync(CancellationToken ct = default)
    {
        var tenantId = TenantId(); var users = await db.Users.AsNoTracking().Where(u => u.TenantId == tenantId).Include(u => u.Roles).ThenInclude(r => r.Role).OrderByDescending(u => u.IsActive).ThenBy(u => u.FullName).ToListAsync(ct);
        return users.Select(CompanyUser).ToList();
    }

    public async Task<CompanyUserDto> CreateUserAsync(Guid actorId, CreateCompanyUserDto dto, CancellationToken ct = default)
    {
        var minimum=systemSettings is null?8:await systemSettings.IntAsync("password-policy","minLength",8,ct);if (dto.Password.Length < minimum) throw ApiException.BadRequest($"كلمة المرور يجب ألا تقل عن {minimum} أحرف");
        var phone = OtpService.NormalizePhone(dto.Phone); var email = Clean(dto.Email, 200)?.ToLowerInvariant();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Phone == phone || (email != null && u.Email == email), ct)) throw ApiException.Conflict("رقم الهاتف أو البريد مسجل بالفعل");
        await ValidateBranch(dto.DefaultBranchId, ct); var roles = await ValidRoles(dto.RoleIds, ct); if (roles.Count == 0 || roles.Count != dto.RoleIds.Distinct().Count()) throw ApiException.BadRequest("اختر أدوارًا صالحة");
        var user = new User { TenantId = TenantId(), FullName = Required(dto.FullName, 160, "الاسم مطلوب"), Phone = phone, Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), JobTitle = Clean(dto.JobTitle, 120), Department = Clean(dto.Department, 120), PurchaseLimit = Positive(dto.PurchaseLimit), DefaultBranchId = dto.DefaultBranchId, PhoneVerified = false };
        foreach (var role in roles) user.Roles.Add(new UserRole { UserId = user.Id, RoleId = role.Id }); db.Users.Add(user);
        Audit(actorId, "company.user_created", nameof(User), user.Id, new { user.FullName }); await db.SaveChangesAsync(ct); return CompanyUser(user);
    }

    public async Task<CompanyUserDto> UpdateUserAsync(Guid actorId, Guid id, UpdateCompanyUserDto dto, CancellationToken ct = default)
    {
        var user = await TenantUser(id, true, ct); if (actorId == id && !dto.IsActive) throw ApiException.Conflict("لا يمكنك تعطيل حسابك الحالي");
        await ValidateBranch(dto.DefaultBranchId, ct); var roles = await ValidRoles(dto.RoleIds, ct); if (roles.Count == 0 || roles.Count != dto.RoleIds.Distinct().Count()) throw ApiException.BadRequest("اختر أدوارًا صالحة");
        var email = Clean(dto.Email, 200)?.ToLowerInvariant(); if (email != user.Email && email is not null && await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email && u.Id != id, ct)) throw ApiException.Conflict("البريد الإلكتروني مستخدم بالفعل");
        user.FullName = Required(dto.FullName, 160, "الاسم مطلوب"); user.Email = email; user.IsActive = dto.IsActive; user.JobTitle = Clean(dto.JobTitle, 120); user.Department = Clean(dto.Department, 120); user.PurchaseLimit = Positive(dto.PurchaseLimit); user.DefaultBranchId = dto.DefaultBranchId;
        var selectedIds = roles.Select(r => r.Id).ToHashSet();
        foreach (var existing in user.Roles.Where(x => !selectedIds.Contains(x.RoleId)).ToList()) db.UserRoles.Remove(existing);
        var existingIds = user.Roles.Select(x => x.RoleId).ToHashSet();
        foreach (var role in roles.Where(r => !existingIds.Contains(r.Id))) user.Roles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        if (!user.IsActive) foreach (var token in await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync(ct)) token.RevokedAt = DateTime.UtcNow;
        Audit(actorId, user.IsActive ? "company.user_updated" : "company.user_deactivated", nameof(User), id); await db.SaveChangesAsync(ct); return CompanyUser(user);
    }

    public Task<CompanyUserDto> SetUserActiveAsync(Guid actorId, Guid id, bool active, CancellationToken ct = default) => UpdateActive(actorId, id, active, ct);
    private async Task<CompanyUserDto> UpdateActive(Guid actorId, Guid id, bool active, CancellationToken ct)
    {
        var user = await TenantUser(id, true, ct); if (actorId == id && !active) throw ApiException.Conflict("لا يمكنك تعطيل حسابك الحالي"); user.IsActive = active;
        if (!active) foreach (var token in await db.RefreshTokens.Where(t => t.UserId == id && t.RevokedAt == null).ToListAsync(ct)) token.RevokedAt = DateTime.UtcNow;
        Audit(actorId, active ? "company.user_activated" : "company.user_deactivated", nameof(User), id); await db.SaveChangesAsync(ct); return CompanyUser(user);
    }

    public async Task<InviteResultDto> InviteAsync(Guid actorId, InviteCompanyUserDto dto, CancellationToken ct = default)
    {
        var email = Clean(dto.Email, 200)?.ToLowerInvariant(); var phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : OtpService.NormalizePhone(dto.Phone);
        if (email is null && phone is null) throw ApiException.BadRequest("أدخل البريد الإلكتروني أو رقم الهاتف"); await ValidateBranch(dto.BranchId, ct);
        _ = (await ValidRoles([dto.RoleId], ct)).SingleOrDefault() ?? throw ApiException.BadRequest("الدور غير صالح");
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => (email != null && u.Email == email) || (phone != null && u.Phone == phone), ct)) throw ApiException.Conflict("يوجد مستخدم بهذه البيانات بالفعل");
        if (await db.CompanyInvites.AnyAsync(i => i.Status == CompanyInviteStatus.Pending && i.ExpiresAt > DateTime.UtcNow && ((email != null && i.Email == email) || (phone != null && i.Phone == phone)), ct)) throw ApiException.Conflict("توجد دعوة سارية لهذا المستخدم بالفعل");
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); var company = await CurrentCompany(ct);
        var invite = new CompanyInvite { TenantId = TenantId(), CompanyId = company.Id, InvitedByUserId = actorId, FullName = Required(dto.FullName, 160, "الاسم مطلوب"), Email = email, Phone = phone, RoleId = dto.RoleId, BranchId = dto.BranchId, TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddDays(7) };
        db.CompanyInvites.Add(invite); Audit(actorId, "company.user_invited", nameof(CompanyInvite), invite.Id, new { invite.Email, invite.Phone }); await db.SaveChangesAsync(ct);
        return new(invite.Id, invite.Status.ToString(), invite.ExpiresAt, raw);
    }

    public async Task<ProfileDto> AcceptInviteAsync(AcceptInviteDto dto, CancellationToken ct = default)
    {
        var minimum=systemSettings is null?8:await systemSettings.IntAsync("password-policy","minLength",8,ct);if (dto.Password.Length < minimum) throw ApiException.BadRequest($"كلمة المرور يجب ألا تقل عن {minimum} أحرف"); var now = DateTime.UtcNow;
        var invite = await db.CompanyInvites.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.TokenHash == Hash(dto.Token) && !i.IsDeleted, ct) ?? throw ApiException.NotFound("الدعوة غير صالحة");
        if (invite.Status != CompanyInviteStatus.Pending || invite.ExpiresAt <= now) { invite.Status = CompanyInviteStatus.Expired; await db.SaveChangesAsync(ct); throw ApiException.Conflict("انتهت صلاحية الدعوة"); }
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => (invite.Email != null && u.Email == invite.Email) || (invite.Phone != null && u.Phone == invite.Phone), ct)) throw ApiException.Conflict("الحساب مسجل بالفعل");
        var user = new User { TenantId = invite.TenantId, FullName = invite.FullName, Email = invite.Email, Phone = invite.Phone ?? $"invite-{invite.Id:N}", PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), DefaultBranchId = invite.BranchId, EmailVerified = invite.Email is not null };
        user.Roles.Add(new UserRole { UserId = user.Id, RoleId = invite.RoleId }); db.Users.Add(user); invite.Status = CompanyInviteStatus.Accepted; invite.AcceptedAt = now;
        AuditFor(invite.TenantId, user.Id, "company.invite_accepted", nameof(CompanyInvite), invite.Id); await db.SaveChangesAsync(ct);
        await db.Entry(user).Collection(u => u.Roles).Query().Include(r => r.Role).LoadAsync(ct); return Profile(user);
    }

    public async Task<List<PermissionDto>> PermissionsAsync(CancellationToken ct = default) => await db.Permissions.AsNoTracking().OrderBy(p => p.Module).ThenBy(p => p.Code).Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.DescriptionAr)).ToListAsync(ct);

    public async Task<List<CompanyRoleDto>> RolesAsync(CancellationToken ct = default)
    {
        var tenantId = TenantId(); var roles = await db.Roles.AsNoTracking().Where(r => r.TenantId == tenantId || (r.TenantId == null && ClientSystemRoles.Contains(r.Code))).Include(r => r.Permissions).ThenInclude(p => p.Permission).OrderByDescending(r => r.IsSystem).ThenBy(r => r.NameAr).ToListAsync(ct);
        var counts = await db.UserRoles.AsNoTracking().Where(x => x.User.TenantId == tenantId).GroupBy(x => x.RoleId).Select(g => new { Id = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        return roles.Select(r => Role(r, counts.GetValueOrDefault(r.Id))).ToList();
    }

    public async Task<CompanyRoleDto> CreateRoleAsync(Guid actorId, CreateCompanyRoleDto dto, CancellationToken ct = default)
    {
        var permissionIds = dto.PermissionIds.Distinct().ToList(); if (permissionIds.Count == 0 || await db.Permissions.CountAsync(p => permissionIds.Contains(p.Id), ct) != permissionIds.Count) throw ApiException.BadRequest("الصلاحيات غير صالحة");
        var role = new Role { TenantId = TenantId(), Code = $"custom_{Guid.NewGuid():N}"[..15], NameAr = Required(dto.NameAr, 120, "اسم الدور مطلوب"), NameEn = Clean(dto.NameEn, 120) ?? dto.NameAr.Trim(), IsSystem = false };
        foreach (var id in permissionIds) role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = id }); db.Roles.Add(role); Audit(actorId, "company.role_created", nameof(Role), role.Id); await db.SaveChangesAsync(ct);
        await db.Entry(role).Collection(r => r.Permissions).Query().Include(p => p.Permission).LoadAsync(ct); return Role(role, 0);
    }

    public async Task<CompanyRoleDto> UpdateRolePermissionsAsync(Guid actorId, Guid id, UpdateRolePermissionsDto dto, CancellationToken ct = default)
    {
        var role = await db.Roles.Include(r => r.Permissions).ThenInclude(p => p.Permission).FirstOrDefaultAsync(r => r.Id == id && r.TenantId == TenantId(), ct) ?? throw ApiException.Forbidden("لا يمكن تعديل دور نظامي");
        var ids = dto.PermissionIds.Distinct().ToList(); if (await db.Permissions.CountAsync(p => ids.Contains(p.Id), ct) != ids.Count) throw ApiException.BadRequest("الصلاحيات غير صالحة");
        db.RolePermissions.RemoveRange(role.Permissions); role.Permissions.Clear(); foreach (var pid in ids) role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid }); Audit(actorId, "company.role_permissions_updated", nameof(Role), id); await db.SaveChangesAsync(ct);
        await db.Entry(role).Collection(r => r.Permissions).Query().Include(p => p.Permission).LoadAsync(ct); var count = await db.UserRoles.CountAsync(x => x.RoleId == id && x.User.TenantId == TenantId(), ct); return Role(role, count);
    }

    public async Task<List<ApprovalPolicyAccountDto>> ApprovalPoliciesAsync(CancellationToken ct = default)
    {
        var policies = await db.ApprovalPolicies.AsNoTracking().Include(p => p.Levels).ThenInclude(l => l.Assignments).OrderBy(p => p.MinimumAmount).ToListAsync(ct); return policies.Select(Policy).ToList();
    }

    public async Task<ApprovalPolicyAccountDto> UpdateApprovalPolicyAsync(Guid actorId, Guid id, UpdateApprovalPolicyDto dto, CancellationToken ct = default)
    {
        var policy = await db.ApprovalPolicies.Include(p => p.Levels).ThenInclude(l => l.Assignments).FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw ApiException.NotFound("سياسة الموافقة غير موجودة");
        if (dto.Levels.Count == 0 || !dto.Levels.Select(l => l.Sequence).Order().SequenceEqual(Enumerable.Range(1, dto.Levels.Count))) throw ApiException.BadRequest("مستويات الموافقة يجب أن تكون متسلسلة من 1");
        var userIds = dto.Levels.SelectMany(l => l.ApproverUserIds).Distinct().ToList(); if (await db.Users.CountAsync(u => u.TenantId == TenantId() && userIds.Contains(u.Id) && u.IsActive, ct) != userIds.Count) throw ApiException.BadRequest("أحد مسؤولي الموافقة غير صالح");
        policy.NameAr = Required(dto.Name, 160, "اسم السياسة مطلوب"); policy.MinimumAmount = Math.Max(0, dto.MinimumAmount); policy.AppliesOnBudgetConflict = dto.AppliesOnBudgetConflict; policy.IsActive = dto.IsActive;
        var sources = dto.Levels.OrderBy(l => l.Sequence).ToList(); var existingLevels = policy.Levels.Where(l => !l.IsDeleted).OrderBy(l => l.Sequence).ToList();
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i]; var level = i < existingLevels.Count ? existingLevels[i] : new ApprovalLevel { TenantId = TenantId(), PolicyId = policy.Id };
            if (i >= existingLevels.Count) db.ApprovalLevels.Add(level);
            level.Sequence = source.Sequence; level.NameAr = Required(source.Name, 120, "اسم المستوى مطلوب"); level.AuthorityLimit = Positive(source.AuthorityLimit); level.SlaHours = Math.Clamp(source.SlaHours, 1, 720);
            var wanted = source.ApproverUserIds.Distinct().ToHashSet(); foreach (var old in level.Assignments.Where(a => !wanted.Contains(a.UserId)).ToList()) db.ApprovalAssignments.Remove(old);
            var assigned = level.Assignments.Select(a => a.UserId).ToHashSet(); foreach (var uid in wanted.Where(uid => !assigned.Contains(uid))) db.ApprovalAssignments.Add(new ApprovalAssignment { TenantId = TenantId(), LevelId = level.Id, UserId = uid });
        }
        foreach (var extra in existingLevels.Skip(sources.Count)) db.ApprovalLevels.Remove(extra);
        Audit(actorId, "company.approval_policy_updated", nameof(ApprovalPolicy), id); await db.SaveChangesAsync(ct); return Policy(policy);
    }

    public async Task<List<CostCenterAccountDto>> CostCentersAsync(CancellationToken ct = default) => await db.CostCenters.AsNoTracking().OrderBy(c => c.Code).Select(c => CostCenter(c)).ToListAsync(ct);

    public async Task<CostCenterAccountDto> CreateCostCenterAsync(Guid actorId, UpsertCostCenterDto dto, CancellationToken ct = default)
    {
        var code = Required(dto.Code, 30, "الكود مطلوب").ToUpperInvariant(); if (await db.CostCenters.AnyAsync(c => c.Code == code, ct)) throw ApiException.Conflict("كود مركز التكلفة مستخدم بالفعل");
        var c = new CostCenter { TenantId = TenantId(), Code = code }; Apply(c, dto); db.CostCenters.Add(c); Audit(actorId, "company.cost_center_created", nameof(CostCenter), c.Id); await db.SaveChangesAsync(ct); return CostCenter(c);
    }

    public async Task<CostCenterAccountDto> UpdateCostCenterAsync(Guid actorId, Guid id, UpsertCostCenterDto dto, CancellationToken ct = default)
    {
        var c = await db.CostCenters.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("مركز التكلفة غير موجود"); var code = Required(dto.Code, 30, "الكود مطلوب").ToUpperInvariant();
        if (code != c.Code && await db.CostCenters.AnyAsync(x => x.Code == code && x.Id != id, ct)) throw ApiException.Conflict("كود مركز التكلفة مستخدم بالفعل"); c.Code = code; Apply(c, dto); Audit(actorId, "company.cost_center_updated", nameof(CostCenter), id); await db.SaveChangesAsync(ct); return CostCenter(c);
    }

    public async Task<List<AccountAuditDto>> AuditAsync(Guid? userId, CancellationToken ct = default)
    {
        var tenantId = TenantId(); var query = db.AuditLogs.AsNoTracking().Where(a => a.TenantId == tenantId); if (userId is not null) query = query.Where(a => a.UserId == userId);
        var logs = await query.OrderByDescending(a => a.AtUtc).Take(200).ToListAsync(ct); var names = await db.Users.AsNoTracking().Where(u => u.TenantId == tenantId).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        return logs.Select(a => new AccountAuditDto(a.Id, a.AtUtc, a.UserId, a.UserId is { } uid ? names.GetValueOrDefault(uid) : null, a.Action, a.EntityType, a.EntityId, a.DataJson)).ToList();
    }

    public async Task<BrandProfileDto> BrandAsync(CancellationToken ct = default)
    {
        var b = await db.CompanyBrandProfiles.AsNoTracking().SingleOrDefaultAsync(ct); return b is null ? new(null, "#11327A", "#F4A024", null, null) : Brand(b);
    }

    public async Task<BrandProfileDto> UpdateBrandAsync(Guid actorId, UpdateBrandProfileDto dto, CancellationToken ct = default)
    {
        if (!Color(dto.PrimaryColor) || !Color(dto.SecondaryColor)) throw ApiException.BadRequest("كود اللون غير صالح"); var company = await CurrentCompany(ct); var b = await db.CompanyBrandProfiles.SingleOrDefaultAsync(ct) ?? new CompanyBrandProfile { TenantId = TenantId(), CompanyId = company.Id };
        if (db.Entry(b).State == EntityState.Detached) db.CompanyBrandProfiles.Add(b); b.PrimaryColor = dto.PrimaryColor.ToUpperInvariant(); b.SecondaryColor = dto.SecondaryColor.ToUpperInvariant(); b.BrandNameAr = Clean(dto.BrandNameAr, 160); b.BrandNameEn = Clean(dto.BrandNameEn, 160);
        Audit(actorId, "company.brand_updated", nameof(CompanyBrandProfile), b.Id); await db.SaveChangesAsync(ct); return Brand(b);
    }

    public async Task<BrandProfileDto> UploadLogoAsync(Guid actorId, IFormFile file, CancellationToken ct = default)
    {
        if (file.Length is 0 or > 5_242_880 || !LogoTypes.Contains(file.ContentType)) throw ApiException.BadRequest("الشعار يجب أن يكون PNG أو JPG أو SVG وبحد أقصى 5 ميجابايت");
        var ext = file.ContentType == "image/png" ? ".png" : file.ContentType == "image/jpeg" ? ".jpg" : ".svg"; var company = await CurrentCompany(ct); var dir = Path.Combine(env.ContentRootPath, "storage", "tenants", TenantId().ToString(), "brand"); Directory.CreateDirectory(dir);
        var stored = $"logo_{Guid.NewGuid():N}{ext}"; await using (var stream = File.Create(Path.Combine(dir, stored))) await file.CopyToAsync(stream, ct);
        var b = await db.CompanyBrandProfiles.SingleOrDefaultAsync(ct) ?? new CompanyBrandProfile { TenantId = TenantId(), CompanyId = company.Id }; if (db.Entry(b).State == EntityState.Detached) db.CompanyBrandProfiles.Add(b); b.LogoPath = Path.Combine("storage", "tenants", TenantId().ToString(), "brand", stored).Replace('\\', '/');
        Audit(actorId, "company.logo_uploaded", nameof(CompanyBrandProfile), b.Id); await db.SaveChangesAsync(ct); return Brand(b);
    }

    public async Task<BillingProfileDto> BillingAsync(CancellationToken ct = default)
    {
        var b = await db.CompanyBillingProfiles.AsNoTracking().SingleOrDefaultAsync(ct); var c = await CurrentCompany(ct); return b is null ? new(c.LegalName, c.Email, c.TaxCardNo, c.AddressLine, 30, false) : Billing(b);
    }

    public async Task<BillingProfileDto> UpdateBillingAsync(Guid actorId, UpdateBillingProfileDto dto, CancellationToken ct = default)
    {
        if (dto.PaymentTermsDays is < 0 or > 365) throw ApiException.BadRequest("مدة السداد غير صالحة"); var company = await CurrentCompany(ct); var b = await db.CompanyBillingProfiles.SingleOrDefaultAsync(ct) ?? new CompanyBillingProfile { TenantId = TenantId(), CompanyId = company.Id };
        if (db.Entry(b).State == EntityState.Detached) db.CompanyBillingProfiles.Add(b); b.InvoiceLegalName = Required(dto.InvoiceLegalName, 240, "اسم الفاتورة مطلوب"); b.BillingEmail = Clean(dto.BillingEmail, 200)?.ToLowerInvariant(); b.TaxRegistrationNo = Clean(dto.TaxRegistrationNo, 100); b.TaxAddress = Clean(dto.TaxAddress, 500); b.PaymentTermsDays = dto.PaymentTermsDays; b.PurchaseOrderRequired = dto.PurchaseOrderRequired;
        Audit(actorId, "company.billing_updated", nameof(CompanyBillingProfile), b.Id); await db.SaveChangesAsync(ct); return Billing(b);
    }

    public async Task<List<CompanyContractDto>> ContractsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow; var contracts = await db.CompanyContracts.OrderByDescending(c => c.EndsAt).ToListAsync(ct); foreach (var c in contracts) { var next = c.EndsAt <= now ? CompanyContractStatus.Expired : c.EndsAt <= now.AddDays(30) ? CompanyContractStatus.Expiring : c.Status; if (c.Status is CompanyContractStatus.Active or CompanyContractStatus.Expiring) c.Status = next; } if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct); return contracts.Select(Contract).ToList();
    }

    public async Task<RenewalRequestDto> RequestRenewalAsync(Guid userId, Guid contractId, CreateRenewalRequestDto dto, CancellationToken ct = default)
    {
        if (dto.RequestedMonths is < 1 or > 60) throw ApiException.BadRequest("مدة التجديد يجب أن تكون بين شهر و60 شهرًا"); var contract = await db.CompanyContracts.FirstOrDefaultAsync(c => c.Id == contractId, ct) ?? throw ApiException.NotFound("العقد غير موجود");
        if (await db.ContractRenewalRequests.AnyAsync(r => r.ContractId == contractId && (r.Status == ContractRenewalStatus.Submitted || r.Status == ContractRenewalStatus.UnderReview), ct)) throw ApiException.Conflict("يوجد طلب تجديد قيد المراجعة");
        var r = new ContractRenewalRequest { TenantId = TenantId(), ContractId = contract.Id, RequestedByUserId = userId, RequestedMonths = dto.RequestedMonths, Note = Clean(dto.Note, 1000) }; db.ContractRenewalRequests.Add(r); Audit(userId, "company.contract_renewal_requested", nameof(CompanyContract), contract.Id); await db.SaveChangesAsync(ct); return Renewal(r);
    }

    private Guid TenantId() => tenantProvider.TenantId ?? throw ApiException.Forbidden("هذا الحساب غير مرتبط بشركة");
    private async Task<Company> CurrentCompany(CancellationToken ct) { var tid = TenantId(); return await db.Companies.FirstOrDefaultAsync(c => c.TenantId == tid, ct) ?? throw ApiException.NotFound("بيانات الشركة غير موجودة"); }
    private async Task<User> TenantUser(Guid id, bool includeRoles, CancellationToken ct) { var q = db.Users.Where(u => u.TenantId == TenantId()); if (includeRoles) q = q.Include(u => u.Roles).ThenInclude(r => r.Role); return await q.FirstOrDefaultAsync(u => u.Id == id, ct) ?? throw ApiException.NotFound("المستخدم غير موجود"); }
    private async Task<List<Role>> ValidRoles(IEnumerable<Guid> ids, CancellationToken ct) { var distinct = ids.Distinct().ToList(); var tid = TenantId(); return await db.Roles.Where(r => distinct.Contains(r.Id) && (r.TenantId == tid || (r.TenantId == null && ClientSystemRoles.Contains(r.Code)))).ToListAsync(ct); }
    private async Task ValidateBranch(Guid? id, CancellationToken ct) { if (id is not null && !await db.CompanyBranches.AnyAsync(b => b.Id == id, ct)) throw ApiException.BadRequest("الفرع غير صالح"); }
    private void Audit(Guid actor, string action, string entity, Guid id, object? data = null) => AuditFor(TenantId(), actor, action, entity, id, data);
    private void AuditFor(Guid tid, Guid actor, string action, string entity, Guid id, object? data = null) => db.AuditLogs.Add(new AuditLog { TenantId = tid, UserId = actor, Action = action, EntityType = entity, EntityId = id.ToString(), DataJson = data is null ? null : JsonSerializer.Serialize(data) });
    private static ProfileDto Profile(User u) => new(u.Id, u.FullName, u.Phone, u.Email, u.AvatarPath, u.PreferredLanguage, u.JobTitle, u.Department, u.PurchaseLimit, u.DefaultBranchId, u.Roles.Select(r => r.Role.Code).ToList());
    private static CompanyDto Company(Company c, string status) => new(c.Id, c.LegalName, c.LegalNameEn, c.CommercialRegistrationNo, c.TaxCardNo, c.Phone, c.Email, c.Governorate, c.City, c.AddressLine, c.Industry, c.EmployeeCountRange, status);
    private static BranchDto Branch(CompanyBranch b) => new(b.Id, b.Name, b.Governorate, b.City, b.AddressLine, b.Phone, b.Latitude, b.Longitude, b.IsMain);
    private static CompanyUserDto CompanyUser(User u) => new(u.Id, u.FullName, u.Phone, u.Email, u.IsActive, u.JobTitle, u.Department, u.PurchaseLimit, u.DefaultBranchId, u.Roles.Select(r => new RoleBriefDto(r.Role.Id, r.Role.Code, r.Role.NameAr)).ToList(), u.CreatedAt);
    private static CompanyRoleDto Role(Role r, int count) => new(r.Id, r.Code, r.NameAr, r.NameEn, r.IsSystem, count, r.Permissions.Select(p => p.Permission.Code).Order().ToList());
    private static ApprovalPolicyAccountDto Policy(ApprovalPolicy p) => new(p.Id, p.NameAr, p.MinimumAmount, p.AppliesOnBudgetConflict, p.IsActive, p.Levels.Where(l => !l.IsDeleted).OrderBy(l => l.Sequence).Select(l => new ApprovalLevelAccountDto(l.Id, l.Sequence, l.NameAr, l.AuthorityLimit, l.SlaHours, l.Assignments.Where(a => !a.IsDeleted).Select(a => a.UserId).ToList())).ToList());
    private static CostCenterAccountDto CostCenter(CostCenter c) => new(c.Id, c.Code, c.NameAr, c.BudgetAmount, c.UsedAmount, c.ReservedAmount, c.ApprovalThreshold, c.IsActive);
    private static BrandProfileDto Brand(CompanyBrandProfile b) => new(b.LogoPath, b.PrimaryColor, b.SecondaryColor, b.BrandNameAr, b.BrandNameEn);
    private static BillingProfileDto Billing(CompanyBillingProfile b) => new(b.InvoiceLegalName, b.BillingEmail, b.TaxRegistrationNo, b.TaxAddress, b.PaymentTermsDays, b.PurchaseOrderRequired);
    private static CompanyContractDto Contract(CompanyContract c) => new(c.Id, c.Number, c.StartsAt, c.EndsAt, c.Status.ToString(), c.PaymentTermsDays, c.CreditLimit, c.AutoRenew, c.TermsSummary, c.DocumentPath, Math.Max(0, (int)Math.Ceiling((c.EndsAt - DateTime.UtcNow).TotalDays)));
    private static RenewalRequestDto Renewal(ContractRenewalRequest r) => new(r.Id, r.ContractId, r.RequestedMonths, r.Status.ToString(), r.CreatedAt, r.Note);
    private static void Apply(CompanyBranch b, UpsertBranchDto d) { b.Name = Required(d.Name, 120, "اسم الفرع مطلوب"); b.Governorate = Clean(d.Governorate, 100); b.City = Clean(d.City, 100); b.AddressLine = Clean(d.AddressLine, 500); b.Phone = Clean(d.Phone, 30); b.Latitude = d.Latitude; b.Longitude = d.Longitude; b.IsMain = d.IsMain; }
    private static void Apply(CostCenter c, UpsertCostCenterDto d) { if (d.Budget < c.UsedAmount + c.ReservedAmount) throw ApiException.BadRequest("الميزانية لا يمكن أن تقل عن المستخدم والمحجوز"); c.NameAr = Required(d.Name, 160, "اسم مركز التكلفة مطلوب"); c.BudgetAmount = d.Budget; c.ApprovalThreshold = Positive(d.ApprovalThreshold); c.IsActive = d.IsActive; }
    private static string Required(string? value, int max, string error) => Clean(value, max) ?? throw ApiException.BadRequest(error);
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var v = value.Trim(); return v.Length <= max ? v : v[..max]; }
    private static decimal? Positive(decimal? value) => value is > 0 ? value : null;
    private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    private static bool Color(string value) => value.Length == 7 && value[0] == '#' && value[1..].All(Uri.IsHexDigit);
}
