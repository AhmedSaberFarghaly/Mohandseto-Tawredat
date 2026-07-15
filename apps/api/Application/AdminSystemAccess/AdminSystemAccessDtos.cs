namespace Mohandseto.Api.Application.AdminSystemAccess;

public sealed record SystemUserRowDto(Guid Id, string FullName, string Phone, string? Email, string? JobTitle,
    string? Department, bool IsActive, bool TwoFactorEnabled, DateTime? SuspendedAt, DateTime? SuspendedUntil,
    string? SuspensionReason, DateTime CreatedAt, IReadOnlyList<string> Roles, int ActiveSessions);
public sealed record SystemRoleDto(Guid Id, string Code, string NameAr, string NameEn, bool IsSystem,
    int UserCount, IReadOnlyList<int> PermissionIds);
public sealed record SystemPermissionDto(int Id, string Code, string Module, string DescriptionAr, string Action);
public sealed record SystemScopeOptionDto(Guid Id, string Name, string? Context);
public sealed record UserScopeDto(Guid UserId, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);
public sealed record LoginAuditDto(Guid Id, Guid? UserId, string? UserName, string Identifier, bool Succeeded,
    string? FailureReason, string? IpAddress, string? Device, string? Location, DateTime At);
public sealed record ActiveSessionDto(Guid Id, Guid UserId, string UserName, string? Device, string? IpAddress,
    DateTime CreatedAt, DateTime LastSeenAt, DateTime ExpiresAt, bool IsCurrent);
public sealed record SystemAuditRowDto(long Id, DateTime At, Guid? ActorId, string? ActorName, string Action,
    string EntityType, string? EntityId, string? Ip);
public sealed record SystemAuditDetailDto(long Id, DateTime At, Guid? ActorId, string? ActorName, string Action,
    string EntityType, string? EntityId, string? Ip, string? DataJson);
public sealed record SystemAccessKpisDto(int ActiveUsers, int Roles, int ActiveSessions, int SuspendedUsers);
public sealed record SystemAccessDashboardDto(SystemAccessKpisDto Kpis, IReadOnlyList<SystemUserRowDto> Users,
    IReadOnlyList<SystemRoleDto> Roles, IReadOnlyList<SystemPermissionDto> Permissions,
    IReadOnlyList<SystemScopeOptionDto> Branches, IReadOnlyList<SystemScopeOptionDto> Warehouses,
    IReadOnlyList<UserScopeDto> Scopes, IReadOnlyList<LoginAuditDto> LoginAudits,
    IReadOnlyList<ActiveSessionDto> Sessions, IReadOnlyList<SystemAuditRowDto> AuditLogs);

public sealed record SaveSystemUserDto(string FullName, string Phone, string Email, string? Password,
    string? JobTitle, string? Department, bool IsActive, bool TwoFactorEnabled, IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);
public sealed record SaveSystemRoleDto(string Code, string NameAr, string NameEn, IReadOnlyList<int> PermissionIds);
public sealed record SaveUserScopesDto(IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds);
public sealed record SuspendSystemUserDto(string Reason, DateTime? Until);
public sealed record AdminResetPasswordDto(string NewPassword);
