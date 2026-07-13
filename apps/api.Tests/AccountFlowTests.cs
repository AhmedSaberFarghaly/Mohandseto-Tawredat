using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.Account;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class AccountFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly AccountService _service;
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    public AccountFlowTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant); _db.Database.EnsureCreated();
        _service = new AccountService(_db, _tenant, new TestEnvironment(Path.GetTempPath()));
    }

    [Fact]
    public async Task Company_account_users_roles_policies_and_contract_are_managed_end_to_end()
    {
        var tenantId = Guid.NewGuid(); var actorId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Acme", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "Acme Supplies", Phone = "+201001234567", CreditLimit = 100000, CreditUsed = 25000 };
        var branch = new CompanyBranch { TenantId = tenantId, CompanyId = company.Id, Name = "الرئيسي", IsMain = true };
        var permission = new Permission { Code = "orders.create", Module = "Orders", DescriptionAr = "إنشاء الطلبات" };
        var role = new Role { Code = "company_admin", NameAr = "مسؤول إداري", NameEn = "Company Admin", IsSystem = true };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, Permission = permission });
        var actor = new User { Id = actorId, TenantId = tenantId, FullName = "مدير الشركة", Phone = "+201000000111", PasswordHash = "x", DefaultBranchId = branch.Id };
        actor.Roles.Add(new UserRole { UserId = actor.Id, RoleId = role.Id });
        var policy = new ApprovalPolicy { TenantId = tenantId, NameAr = "موافقة المشتريات" };
        policy.Levels.Add(new ApprovalLevel { TenantId = tenantId, Sequence = 1, NameAr = "المدير", Assignments = [new ApprovalAssignment { TenantId = tenantId, UserId = actorId }] });
        var center = new CostCenter { TenantId = tenantId, Code = "OPS", NameAr = "التشغيل", BudgetAmount = 50000 };
        var contract = new CompanyContract { TenantId = tenantId, CompanyId = company.Id, Number = $"CTR-{Guid.NewGuid():N}", StartsAt = DateTime.UtcNow.AddMonths(-3), EndsAt = DateTime.UtcNow.AddDays(20), CreditLimit = 100000, TermsSummary = "توريد لمدة عام" };
        _db.AddRange(tenant, company, branch, role, actor, policy, center, contract); await _db.SaveChangesAsync();

        var overview = await _service.OverviewAsync(actorId); Assert.Equal(100000, overview.CreditLimit); Assert.Equal(1, overview.Users);
        var created = await _service.CreateUserAsync(actorId, new("مسؤول مشتريات", "+201000000222", "buyer@acme.test", "Strong@123", "مشترٍ", "المشتريات", 15000, branch.Id, [role.Id]));
        Assert.Equal(15000, created.PurchaseLimit); Assert.Single(created.Roles);
        created = await _service.UpdateUserAsync(actorId, created.Id, new(created.FullName, created.Email, true, created.JobTitle, created.Department, 20000, branch.Id, [role.Id])); Assert.Equal(20000, created.PurchaseLimit);

        var invite = await _service.InviteAsync(actorId, new("محاسب الشركة", "accountant@acme.test", null, role.Id, branch.Id)); Assert.Equal("Pending", invite.Status);
        var accepted = await _service.AcceptInviteAsync(new(invite.InvitationToken, "Account@123")); Assert.Equal("accountant@acme.test", accepted.Email);
        Assert.Equal(3, (await _service.UsersAsync()).Count);

        var roles = await _service.RolesAsync(); Assert.Single(roles); Assert.Contains("orders.create", roles[0].Permissions);
        var updatedPolicy = await _service.UpdateApprovalPolicyAsync(actorId, policy.Id, new("موافقة متدرجة", 1000, true, true, [new(null, 1, "مدير الإدارة", 25000, 12, [actorId]), new(null, 2, "المدير المالي", null, 24, [created.Id])]));
        Assert.Equal(2, updatedPolicy.Levels.Count);
        var updatedCenter = await _service.UpdateCostCenterAsync(actorId, center.Id, new("OPS", "تشغيل ومشتريات", 75000, 10000, true)); Assert.Equal(75000, updatedCenter.Budget);
        var billing = await _service.UpdateBillingAsync(actorId, new("Acme Supplies", "billing@acme.test", "TAX-99", "القاهرة", 45, true)); Assert.True(billing.PurchaseOrderRequired);
        var brand = await _service.UpdateBrandAsync(actorId, new("#123456", "#ABCDEF", "أكمي", "ACME")); Assert.Equal("#123456", brand.PrimaryColor);
        var contracts = await _service.ContractsAsync(); Assert.Equal("Expiring", contracts.Single().Status);
        var renewal = await _service.RequestRenewalAsync(actorId, contract.Id, new(24, "تجديد لعامين")); Assert.Equal("Submitted", renewal.Status);
        Assert.Contains(await _service.AuditAsync(null), a => a.Action == "company.contract_renewal_requested");

        _tenant.TenantId = Guid.NewGuid(); Assert.Empty(await _service.UsersAsync()); Assert.Empty(await _service.BranchesAsync());
    }

    [Fact]
    public async Task User_deactivation_revokes_active_sessions()
    {
        var tenantId = Guid.NewGuid(); var actorId = Guid.NewGuid(); var userId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var tenant = new Tenant { Id = tenantId, Name = "Secure Co", Status = TenantStatus.Active }; var company = new Company { TenantId = tenantId, LegalName = "Secure Co", Phone = "+201009990001" };
        var role = new Role { Code = "company_admin", NameAr = "مدير", NameEn = "Admin", IsSystem = true };
        var actor = new User { Id = actorId, TenantId = tenantId, FullName = "Admin", Phone = "+201009990002" }; actor.Roles.Add(new UserRole { UserId = actorId, RoleId = role.Id });
        var user = new User { Id = userId, TenantId = tenantId, FullName = "Buyer", Phone = "+201009990003" }; user.Roles.Add(new UserRole { UserId = userId, RoleId = role.Id });
        var token = new RefreshToken { UserId = userId, TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = DateTime.UtcNow.AddDays(2) };
        _db.AddRange(tenant, company, role, actor, user, token); await _db.SaveChangesAsync();
        var result = await _service.SetUserActiveAsync(actorId, userId, false); Assert.False(result.IsActive); Assert.NotNull(token.RevokedAt);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }
}
