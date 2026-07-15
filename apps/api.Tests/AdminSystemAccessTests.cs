using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.AdminSystemAccess;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AdminSystemAccessTests : IDisposable
{
    private sealed class PlatformTenant : ITenantProvider { public Guid? TenantId => null; }
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AdminSystemAccessService _service;
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _systemRoleId = Guid.NewGuid();
    private readonly Guid _superRoleId = Guid.NewGuid();

    public AdminSystemAccessTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, new PlatformTenant());
        _db.Database.EnsureCreated(); _service = new AdminSystemAccessService(_db);
        SeedAsync().GetAwaiter().GetResult();
    }

    private async Task SeedAsync()
    {
        var super = new Role { Id = _superRoleId, Code = "super_admin", NameAr = "مدير رئيسي", NameEn = "Super Admin", IsSystem = true };
        var system = new Role { Id = _systemRoleId, Code = "system_admin", NameAr = "مدير نظام", NameEn = "System Admin", IsSystem = true };
        var actor = new User { Id = _actorId, FullName = "مدير الاختبار", Phone = "01090000001", Email = "root@test.local",
            IsPlatformStaff = true, IsActive = true, Roles = [new UserRole { Role = super }] };
        _db.AddRange(super, system, actor, new Permission { Id = 9001, Code = "users.view", Module = "users", DescriptionAr = "عرض المستخدمين" },
            new Permission { Id = 9002, Code = "users.manage", Module = "users", DescriptionAr = "إدارة المستخدمين" },
            new Warehouse { Code = "TST", NameAr = "مخزن الاختبار", Governorate = "القاهرة", Address = "العنوان" });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Creates_platform_user_with_role_scopes_and_audit()
    {
        var warehouse = await _db.Warehouses.SingleAsync();
        var id = await _service.CreateUserAsync(_actorId, "127.0.0.1", new("مسؤول تشغيل", "01090000002", "ops@test.local",
            "Strong@123", "مشرف", "التشغيل", true, true, [_systemRoleId], [], [warehouse.Id]));

        var user = await _db.Users.Include(x => x.Roles).ThenInclude(x => x.Role).SingleAsync(x => x.Id == id);
        Assert.True(user.IsPlatformStaff); Assert.Null(user.TenantId); Assert.True(user.TwoFactorEnabled);
        Assert.Equal("system_admin", Assert.Single(user.Roles).Role.Code);
        Assert.Equal(warehouse.Id, (await _db.UserAccessScopes.SingleAsync(x => x.UserId == id)).ScopeId);
        Assert.Contains(await _db.AuditLogs.ToListAsync(), x => x.Action == "system_user.created" && x.EntityId == id.ToString());
    }

    [Fact]
    public async Task Suspension_revokes_sessions_and_dashboard_exposes_security_data()
    {
        var userId = await _service.CreateUserAsync(_actorId, null, new("مستخدم قابل للتعليق", "01090000003", "suspend@test.local",
            "Strong@123", null, null, true, false, [_systemRoleId], [], []));
        _db.RefreshTokens.Add(new RefreshToken { UserId = userId, TokenHash = "test-token", ExpiresAt = DateTime.UtcNow.AddDays(1), Device = "Windows · Chrome", IpAddress = "10.0.0.2" });
        _db.LoginAudits.Add(new LoginAudit { UserId = userId, Identifier = "suspend@test.local", Succeeded = true, IpAddress = "10.0.0.2" });
        await _db.SaveChangesAsync();

        await _service.SuspendAsync(_actorId, "127.0.0.1", userId, new("اختبار الأمان", DateTime.UtcNow.AddDays(3)));
        Assert.False((await _db.Users.SingleAsync(x => x.Id == userId)).IsActive);
        Assert.NotNull((await _db.RefreshTokens.SingleAsync(x => x.UserId == userId)).RevokedAt);
        var dashboard = await _service.DashboardAsync(_actorId);
        Assert.Contains(dashboard.Users, x => x.Id == userId && !x.IsActive && x.SuspensionReason == "اختبار الأمان");
        Assert.Contains(dashboard.LoginAudits, x => x.UserId == userId && x.Succeeded);
        Assert.DoesNotContain(dashboard.Sessions, x => x.UserId == userId);
    }

    [Fact]
    public async Task Cannot_suspend_the_last_active_super_admin()
    {
        var secondSuperId = Guid.NewGuid();
        _db.Users.Add(new User { Id = secondSuperId, FullName = "مدير احتياطي غير نشط", Phone = "01090000009",
            Email = "inactive-root@test.local", IsPlatformStaff = true, IsActive = false,
            Roles = [new UserRole { RoleId = _superRoleId }] });
        await _db.SaveChangesAsync();
        var ex = await Assert.ThrowsAsync<ApiException>(() => _service.SuspendAsync(secondSuperId, null, _actorId, new("غير مسموح", null)));
        Assert.Equal(409, ex.StatusCode);
        Assert.True((await _db.Users.SingleAsync(x => x.Id == _actorId)).IsActive);
    }

    [Fact]
    public async Task Custom_role_persists_permission_matrix()
    {
        var id = await _service.CreateRoleAsync(_actorId, null, new("security_auditor", "مدقق الأمان", "Security Auditor", [9001]));
        var role = await _db.Roles.Include(x => x.Permissions).SingleAsync(x => x.Id == id);
        Assert.False(role.IsSystem); Assert.Equal(9001, Assert.Single(role.Permissions).PermissionId);
        await _service.UpdateRoleAsync(_actorId, null, id, new("security_auditor", "مدقق أمان رئيسي", "Senior Security Auditor", [9001, 9002]));
        Assert.Equal(2, await _db.RolePermissions.CountAsync(x => x.RoleId == id));
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
