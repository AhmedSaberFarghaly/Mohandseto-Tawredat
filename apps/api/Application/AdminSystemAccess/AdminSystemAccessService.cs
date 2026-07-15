using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.AdminSystemSettings;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminSystemAccess;

public sealed class AdminSystemAccessService(AppDbContext db, AdminSystemSettingsService? systemSettings = null)
{
    private static readonly string[] PlatformSystemRoleCodes = ["super_admin", "sales_manager", "sales_agent", "quotes_officer",
        "products_manager", "inventory_manager", "warehouse_manager", "procurement_officer", "accountant", "support_agent",
        "graphic_designer", "printing_officer", "delivery_driver", "operations_manager", "system_admin", "auditor"];

    public async Task<SystemAccessDashboardDto> DashboardAsync(Guid currentUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var users = await db.Users.AsNoTracking().Where(x => x.IsPlatformStaff && x.TenantId == null)
            .Include(x => x.Roles).ThenInclude(x => x.Role).OrderBy(x => x.FullName).ToListAsync(ct);
        var roles = await db.Roles.AsNoTracking().Where(x => x.TenantId == null && (!x.IsSystem || PlatformSystemRoleCodes.Contains(x.Code)))
            .Include(x => x.Permissions).OrderBy(x => x.NameAr).ToListAsync(ct);
        var permissions = await db.Permissions.AsNoTracking().OrderBy(x => x.Module).ThenBy(x => x.Code).ToListAsync(ct);
        var activeTokens = await db.RefreshTokens.AsNoTracking().Where(x => x.RevokedAt == null && x.ExpiresAt > now)
            .Include(x => x.User).Where(x => x.User.IsPlatformStaff).OrderByDescending(x => x.LastSeenAt ?? x.CreatedAt).ToListAsync(ct);
        var activeCounts = activeTokens.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.Count());
        var roleCounts = await db.UserRoles.AsNoTracking().Where(x => x.Role.TenantId == null && (!x.Role.IsSystem || PlatformSystemRoleCodes.Contains(x.Role.Code)))
            .GroupBy(x => x.RoleId).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var loginUsers = users.ToDictionary(x => x.Id, x => x.FullName);
        var logins = await db.LoginAudits.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct);
        var scopes = await db.UserAccessScopes.AsNoTracking().Where(x => x.User.IsPlatformStaff).ToListAsync(ct);
        var branches = await db.CompanyBranches.AsNoTracking().Include(x => x.Company).OrderBy(x => x.Name).Take(500).ToListAsync(ct);
        var warehouses = await db.Warehouses.AsNoTracking().OrderBy(x => x.NameAr).ToListAsync(ct);
        var audits = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.AtUtc).Take(250).ToListAsync(ct);
        var actorIds = audits.Where(x => x.UserId != null).Select(x => x.UserId!.Value).Distinct().ToList();
        var actors = await db.Users.AsNoTracking().Where(x => actorIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

        var userDtos = users.Select(x => new SystemUserRowDto(x.Id, x.FullName, x.Phone, x.Email, x.JobTitle,
            x.Department, EffectiveActive(x, now), x.TwoFactorEnabled, x.SuspendedAt, x.SuspendedUntil,
            x.SuspensionReason, x.CreatedAt, x.Roles.Select(r => r.Role.Code).Order().ToList(), activeCounts.GetValueOrDefault(x.Id))).ToList();
        var roleDtos = roles.Select(x => new SystemRoleDto(x.Id, x.Code, x.NameAr, x.NameEn, x.IsSystem,
            roleCounts.GetValueOrDefault(x.Id), x.Permissions.Select(p => p.PermissionId).Order().ToList())).ToList();
        var permissionDtos = permissions.Select(x => new SystemPermissionDto(x.Id, x.Code, x.Module, x.DescriptionAr,
            x.Code.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? x.Code)).ToList();
        var scopeDtos = users.Select(x => new UserScopeDto(x.Id,
            scopes.Where(s => s.UserId == x.Id && s.ScopeType == UserAccessScopeType.Branch).Select(s => s.ScopeId).ToList(),
            scopes.Where(s => s.UserId == x.Id && s.ScopeType == UserAccessScopeType.Warehouse).Select(s => s.ScopeId).ToList())).ToList();
        var loginDtos = logins.Select(x => new LoginAuditDto(x.Id, x.UserId,
            x.UserId is { } uid ? loginUsers.GetValueOrDefault(uid) : null, x.Identifier, x.Succeeded,
            x.FailureReason, x.IpAddress, Device(x.UserAgent), x.Location, x.CreatedAt)).ToList();
        var sessions = activeTokens.Select(x => new ActiveSessionDto(x.Id, x.UserId, x.User.FullName, x.Device,
            x.IpAddress, x.CreatedAt, x.LastSeenAt ?? x.CreatedAt, x.ExpiresAt, x.UserId == currentUserId)).ToList();
        var auditDtos = audits.Select(x => new SystemAuditRowDto(x.Id, x.AtUtc, x.UserId,
            x.UserId is { } uid ? actors.GetValueOrDefault(uid) : null, x.Action, x.EntityType, x.EntityId, x.Ip)).ToList();
        return new(new(userDtos.Count(x => x.IsActive), roleDtos.Count, sessions.Count, userDtos.Count(x => !x.IsActive)),
            userDtos, roleDtos, permissionDtos,
            branches.Select(x => new SystemScopeOptionDto(x.Id, x.Name, x.Company.LegalName)).ToList(),
            warehouses.Select(x => new SystemScopeOptionDto(x.Id, x.NameAr, x.Governorate)).ToList(), scopeDtos,
            loginDtos, sessions, auditDtos);
    }

    public async Task<Guid> CreateUserAsync(Guid actorId, string? ip, SaveSystemUserDto dto, CancellationToken ct = default)
    {
        ValidateUser(dto, requirePassword: true, systemSettings is null?8:await systemSettings.IntAsync("password-policy","minLength",8,ct));
        var email = dto.Email.Trim().ToLowerInvariant(); var phone = dto.Phone.Trim();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == email || x.Phone == phone, ct))
            throw ApiException.Conflict("البريد الإلكتروني أو رقم الهاتف مستخدم بالفعل");
        var roles = await ValidRoles(dto.RoleIds, ct);
        if (roles.Any(x => x.Code == "super_admin")) await EnsureActorIsSuperAdmin(actorId, ct);
        await ValidateScopes(dto.BranchIds, dto.WarehouseIds, ct);
        var user = new User { FullName = dto.FullName.Trim(), Phone = phone, Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password!), JobTitle = Clean(dto.JobTitle), Department = Clean(dto.Department),
            IsActive = dto.IsActive, IsPlatformStaff = true, EmailVerified = true, PhoneVerified = true,
            TwoFactorEnabled = dto.TwoFactorEnabled, TwoFactorChannel = dto.TwoFactorEnabled ? "phone" : null,
            CreatedBy = actorId };
        db.Users.Add(user);
        user.Roles = roles.Select(x => new UserRole { User = user, RoleId = x.Id }).ToList();
        SetScopes(user.Id, dto.BranchIds, dto.WarehouseIds);
        await Audit(actorId, ip, "system_user.created", nameof(User), user.Id.ToString(), null, UserSnapshot(user), ct);
        await db.SaveChangesAsync(ct); return user.Id;
    }

    public async Task UpdateUserAsync(Guid actorId, string? ip, Guid id, SaveSystemUserDto dto, CancellationToken ct = default)
    {
        ValidateUser(dto, requirePassword: false, systemSettings is null?8:await systemSettings.IntAsync("password-policy","minLength",8,ct));
        var user = await db.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id && x.IsPlatformStaff && x.TenantId == null, ct)
            ?? throw ApiException.NotFound("المستخدم غير موجود");
        var email = dto.Email.Trim().ToLowerInvariant(); var phone = dto.Phone.Trim();
        if (await db.Users.IgnoreQueryFilters().AnyAsync(x => x.Id != id && (x.Email == email || x.Phone == phone), ct))
            throw ApiException.Conflict("البريد الإلكتروني أو رقم الهاتف مستخدم بالفعل");
        var roles = await ValidRoles(dto.RoleIds, ct); await ValidateScopes(dto.BranchIds, dto.WarehouseIds, ct);
        if (user.Roles.Any(x => x.Role.Code == "super_admin") || roles.Any(x => x.Code == "super_admin")) await EnsureActorIsSuperAdmin(actorId, ct);
        if (actorId == id && !dto.IsActive) throw ApiException.BadRequest("لا يمكنك تعطيل حسابك الحالي");
        await EnsureSuperAdminRemains(user, roles.Select(x => x.Code), dto.IsActive, ct);
        var before = UserSnapshot(user);
        user.FullName = dto.FullName.Trim(); user.Phone = phone; user.Email = email; user.JobTitle = Clean(dto.JobTitle);
        user.Department = Clean(dto.Department); user.IsActive = dto.IsActive; user.TwoFactorEnabled = dto.TwoFactorEnabled;
        user.TwoFactorChannel = dto.TwoFactorEnabled ? "phone" : null; user.UpdatedAt = DateTime.UtcNow; user.UpdatedBy = actorId;
        if (!string.IsNullOrWhiteSpace(dto.Password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        db.UserRoles.RemoveRange(user.Roles); user.Roles = roles.Select(x => new UserRole { UserId = id, RoleId = x.Id }).ToList();
        await ReplaceScopes(id, dto.BranchIds, dto.WarehouseIds, ct);
        if (!dto.IsActive) await RevokeUserSessions(id, ct);
        await Audit(actorId, ip, "system_user.updated", nameof(User), id.ToString(), before, UserSnapshot(user), ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> CreateRoleAsync(Guid actorId, string? ip, SaveSystemRoleDto dto, CancellationToken ct = default)
    {
        ValidateRole(dto); var code = NormalizeCode(dto.Code);
        if (await db.Roles.AnyAsync(x => x.TenantId == null && x.Code == code, ct)) throw ApiException.Conflict("كود الدور مستخدم بالفعل");
        var permissionIds = await ValidPermissions(dto.PermissionIds, ct);
        var role = new Role { Code = code, NameAr = dto.NameAr.Trim(), NameEn = dto.NameEn.Trim(), IsSystem = false, CreatedBy = actorId,
            Permissions = permissionIds.Select(x => new RolePermission { PermissionId = x }).ToList() };
        db.Roles.Add(role); await Audit(actorId, ip, "role.created", nameof(Role), role.Id.ToString(), null, RoleSnapshot(role), ct);
        await db.SaveChangesAsync(ct); return role.Id;
    }

    public async Task UpdateRoleAsync(Guid actorId, string? ip, Guid id, SaveSystemRoleDto dto, CancellationToken ct = default)
    {
        ValidateRole(dto); var role = await db.Roles.Include(x => x.Permissions).FirstOrDefaultAsync(x => x.Id == id && x.TenantId == null && (!x.IsSystem || PlatformSystemRoleCodes.Contains(x.Code)), ct)
            ?? throw ApiException.NotFound("الدور غير موجود");
        if (role.IsSystem && role.Code == "super_admin") throw ApiException.Forbidden("لا يمكن تعديل صلاحيات مدير النظام الرئيسي");
        var code = NormalizeCode(dto.Code);
        if (await db.Roles.AnyAsync(x => x.Id != id && x.TenantId == null && x.Code == code, ct)) throw ApiException.Conflict("كود الدور مستخدم بالفعل");
        var permissionIds = await ValidPermissions(dto.PermissionIds, ct); var before = RoleSnapshot(role);
        role.Code = code; role.NameAr = dto.NameAr.Trim(); role.NameEn = dto.NameEn.Trim(); role.UpdatedAt = DateTime.UtcNow; role.UpdatedBy = actorId;
        db.RolePermissions.RemoveRange(role.Permissions); role.Permissions = permissionIds.Select(x => new RolePermission { RoleId = id, PermissionId = x }).ToList();
        await Audit(actorId, ip, "role.updated", nameof(Role), id.ToString(), before, RoleSnapshot(role), ct); await db.SaveChangesAsync(ct);
    }

    public async Task SaveScopesAsync(Guid actorId, string? ip, Guid userId, SaveUserScopesDto dto, CancellationToken ct = default)
    {
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsPlatformStaff, ct)) throw ApiException.NotFound("المستخدم غير موجود");
        await ValidateScopes(dto.BranchIds, dto.WarehouseIds, ct);
        var before = await db.UserAccessScopes.Where(x => x.UserId == userId).Select(x => new { x.ScopeType, x.ScopeId }).ToListAsync(ct);
        await ReplaceScopes(userId, dto.BranchIds, dto.WarehouseIds, ct);
        await Audit(actorId, ip, "user_scopes.updated", nameof(UserAccessScope), userId.ToString(), before,
            new { BranchIds = dto.BranchIds.Distinct(), WarehouseIds = dto.WarehouseIds.Distinct() }, ct); await db.SaveChangesAsync(ct);
    }

    public async Task SuspendAsync(Guid actorId, string? ip, Guid userId, SuspendSystemUserDto dto, CancellationToken ct = default)
    {
        if (actorId == userId) throw ApiException.BadRequest("لا يمكنك تعليق حسابك الحالي");
        if (string.IsNullOrWhiteSpace(dto.Reason)) throw ApiException.BadRequest("سبب التعليق مطلوب");
        if (dto.Until is { } until && until.ToUniversalTime() <= DateTime.UtcNow) throw ApiException.BadRequest("تاريخ انتهاء التعليق يجب أن يكون في المستقبل");
        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == userId && x.IsPlatformStaff, ct)
            ?? throw ApiException.NotFound("المستخدم غير موجود");
        if (user.Roles.Any(x => x.Role.Code == "super_admin")) await EnsureActorIsSuperAdmin(actorId, ct);
        await EnsureSuperAdminRemains(user, [], false, ct); var before = UserSnapshot(user);
        user.IsActive = false; user.SuspendedAt = DateTime.UtcNow; user.SuspendedUntil = dto.Until?.ToUniversalTime(); user.SuspensionReason = dto.Reason.Trim();
        await RevokeUserSessions(userId, ct); await Audit(actorId, ip, "system_user.suspended", nameof(User), userId.ToString(), before, UserSnapshot(user), ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ResumeAsync(Guid actorId, string? ip, Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == userId && x.IsPlatformStaff, ct) ?? throw ApiException.NotFound("المستخدم غير موجود");
        if (user.Roles.Any(x => x.Role.Code == "super_admin")) await EnsureActorIsSuperAdmin(actorId, ct);
        var before = UserSnapshot(user); user.IsActive = true; user.SuspendedAt = null; user.SuspendedUntil = null; user.SuspensionReason = null;
        await Audit(actorId, ip, "system_user.resumed", nameof(User), userId.ToString(), before, UserSnapshot(user), ct); await db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(Guid actorId, string? ip, Guid userId, AdminResetPasswordDto dto, CancellationToken ct = default)
    {
        var minimum=systemSettings is null?8:await systemSettings.IntAsync("password-policy","minLength",8,ct);if (dto.NewPassword.Length < minimum) throw ApiException.BadRequest($"كلمة المرور يجب ألا تقل عن {minimum} أحرف");
        var user = await db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == userId && x.IsPlatformStaff, ct) ?? throw ApiException.NotFound("المستخدم غير موجود");
        if (user.Roles.Any(x => x.Role.Code == "super_admin")) await EnsureActorIsSuperAdmin(actorId, ct);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword); user.UpdatedAt = DateTime.UtcNow; user.UpdatedBy = actorId;
        await RevokeUserSessions(userId, ct); await Audit(actorId, ip, "system_user.password_reset", nameof(User), userId.ToString(), null, new { SessionsRevoked = true }, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeSessionAsync(Guid actorId, string? ip, Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == sessionId && x.User.IsPlatformStaff, ct)
            ?? throw ApiException.NotFound("الجلسة غير موجودة");
        session.RevokedAt ??= DateTime.UtcNow; await Audit(actorId, ip, "session.revoked", nameof(RefreshToken), sessionId.ToString(), null, new { session.UserId, session.Device, session.IpAddress }, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task<SystemAuditDetailDto> AuditDetailAsync(long id, CancellationToken ct = default)
    {
        var audit = await db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("سجل التدقيق غير موجود");
        var actor = audit.UserId is { } uid ? await db.Users.AsNoTracking().Where(x => x.Id == uid).Select(x => x.FullName).FirstOrDefaultAsync(ct) : null;
        return new(audit.Id, audit.AtUtc, audit.UserId, actor, audit.Action, audit.EntityType, audit.EntityId, audit.Ip, audit.DataJson);
    }

    private static bool EffectiveActive(User x, DateTime now) => x.IsActive || x.SuspendedUntil is { } until && until <= now;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string NormalizeCode(string value) => value.Trim().ToLowerInvariant().Replace(' ', '_');
    private static object UserSnapshot(User x) => new { x.Id, x.FullName, x.Phone, x.Email, x.JobTitle, x.Department,
        x.IsActive, x.TwoFactorEnabled, x.SuspendedAt, x.SuspendedUntil, x.SuspensionReason,
        Roles = x.Roles.Select(r => r.Role?.Code ?? r.RoleId.ToString()).Order().ToList() };
    private static object RoleSnapshot(Role x) => new { x.Id, x.Code, x.NameAr, x.NameEn, x.IsSystem,
        PermissionIds = x.Permissions.Select(p => p.PermissionId).Order().ToList() };
    private static string? Device(string? ua) => string.IsNullOrWhiteSpace(ua) ? null : ua.Length <= 80 ? ua : ua[..80];
    private static void ValidateUser(SaveSystemUserDto dto, bool requirePassword, int minimumPasswordLength)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrWhiteSpace(dto.Email)) throw ApiException.BadRequest("الاسم والهاتف والبريد الإلكتروني مطلوبة");
        if ((requirePassword || !string.IsNullOrWhiteSpace(dto.Password)) && (dto.Password?.Length ?? 0) < minimumPasswordLength) throw ApiException.BadRequest($"كلمة المرور يجب ألا تقل عن {minimumPasswordLength} أحرف");
        if (dto.RoleIds.Count == 0) throw ApiException.BadRequest("اختر دورًا واحدًا على الأقل");
    }
    private static void ValidateRole(SaveSystemRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.NameAr) || string.IsNullOrWhiteSpace(dto.NameEn)) throw ApiException.BadRequest("كود واسم الدور مطلوبان");
        if (dto.PermissionIds.Count == 0) throw ApiException.BadRequest("اختر صلاحية واحدة على الأقل");
    }
    private async Task<List<Role>> ValidRoles(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var unique = ids.Distinct().ToList(); var roles = await db.Roles.Where(x => x.TenantId == null && (!x.IsSystem || PlatformSystemRoleCodes.Contains(x.Code)) && unique.Contains(x.Id)).ToListAsync(ct);
        if (roles.Count != unique.Count) throw ApiException.BadRequest("أحد الأدوار المحددة غير صالح"); return roles;
    }
    private async Task<List<int>> ValidPermissions(IReadOnlyList<int> ids, CancellationToken ct)
    {
        var unique = ids.Distinct().ToList(); var found = await db.Permissions.Where(x => unique.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct);
        if (found.Count != unique.Count) throw ApiException.BadRequest("إحدى الصلاحيات المحددة غير صالحة"); return found;
    }
    private async Task ValidateScopes(IReadOnlyList<Guid> branchIds, IReadOnlyList<Guid> warehouseIds, CancellationToken ct)
    {
        var branches = branchIds.Distinct().ToList(); var warehouses = warehouseIds.Distinct().ToList();
        if (branches.Count != await db.CompanyBranches.CountAsync(x => branches.Contains(x.Id), ct) || warehouses.Count != await db.Warehouses.CountAsync(x => warehouses.Contains(x.Id), ct))
            throw ApiException.BadRequest("أحد الفروع أو المخازن المحددة غير صالح");
    }
    private void SetScopes(Guid userId, IReadOnlyList<Guid> branches, IReadOnlyList<Guid> warehouses)
    {
        db.UserAccessScopes.AddRange(branches.Distinct().Select(x => new UserAccessScope { UserId = userId, ScopeType = UserAccessScopeType.Branch, ScopeId = x }));
        db.UserAccessScopes.AddRange(warehouses.Distinct().Select(x => new UserAccessScope { UserId = userId, ScopeType = UserAccessScopeType.Warehouse, ScopeId = x }));
    }
    private async Task ReplaceScopes(Guid userId, IReadOnlyList<Guid> branches, IReadOnlyList<Guid> warehouses, CancellationToken ct)
    { db.UserAccessScopes.RemoveRange(await db.UserAccessScopes.Where(x => x.UserId == userId).ToListAsync(ct)); SetScopes(userId, branches, warehouses); }
    private async Task RevokeUserSessions(Guid userId, CancellationToken ct)
    { foreach (var token in await db.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null).ToListAsync(ct)) token.RevokedAt = DateTime.UtcNow; }
    private async Task EnsureSuperAdminRemains(User current, IEnumerable<string> newRoles, bool active, CancellationToken ct)
    {
        var wasSuper = current.Roles.Any(x => x.Role.Code == "super_admin");
        if (!wasSuper || active && newRoles.Contains("super_admin")) return;
        var others = await db.Users.CountAsync(x => x.Id != current.Id && x.IsPlatformStaff && x.IsActive && x.Roles.Any(r => r.Role.Code == "super_admin"), ct);
        if (others == 0) throw ApiException.Conflict("لا يمكن إزالة أو تعليق آخر مدير نظام رئيسي");
    }
    private async Task EnsureActorIsSuperAdmin(Guid actorId, CancellationToken ct)
    {
        if (!await db.UserRoles.AnyAsync(x => x.UserId == actorId && x.Role.Code == "super_admin", ct))
            throw ApiException.Forbidden("إدارة حسابات مدير النظام الرئيسي متاحة لمدير رئيسي فقط");
    }
    private Task Audit(Guid actorId, string? ip, string action, string entityType, string entityId, object? before, object? after, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog { AtUtc = DateTime.UtcNow, UserId = actorId, Action = action, EntityType = entityType, EntityId = entityId,
            Ip = ip, DataJson = JsonSerializer.Serialize(new { before, after }) }); return Task.CompletedTask;
    }
}
