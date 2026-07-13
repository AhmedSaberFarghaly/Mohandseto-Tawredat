using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Mohandseto.Api.Application.Approvals;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Customization;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Tests;

public sealed class ApprovalFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly TestTenantProvider _tenant = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mohandseto-approval-tests", Guid.NewGuid().ToString("N"));
    private sealed class TestTenantProvider : ITenantProvider { public Guid? TenantId { get; set; } }
    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(root);
    }

    public ApprovalFlowTests()
    {
        Directory.CreateDirectory(_root);
        _connection = new SqliteConnection("DataSource=:memory:"); _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options, _tenant);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Approval_supports_changes_delegation_chain_attachments_and_final_budget_commit()
    {
        var tenantId = Guid.NewGuid(); _tenant.TenantId = tenantId;
        var requesterId = Guid.NewGuid(); var delegateId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Approval Co", Status = TenantStatus.Active };
        var company = new Company { TenantId = tenantId, LegalName = "Approval Co", Phone = "01000000000" };
        var branch = new CompanyBranch { TenantId = tenantId, CompanyId = company.Id, Name = "المقر" };
        var requester = new User { Id = requesterId, TenantId = tenantId, FullName = "أحمد المدير", Phone = "01010000001", PhoneVerified = true };
        var delegated = new User { Id = delegateId, TenantId = tenantId, FullName = "منى البديلة", Phone = "01010000002", PhoneVerified = true };
        var center = new CostCenter { TenantId = tenantId, Code = "CC-APR", NameAr = "ميزانية الموافقات", BudgetAmount = 100000, ReservedAmount = 15000 };
        var policy = new ApprovalPolicy { TenantId = tenantId, NameAr = "اعتماد ثلاثي" };
        foreach (var (sequence, name, limit) in new[] { (1, "مدير القسم", (decimal?)5000), (2, "مسؤول الميزانية", (decimal?)20000), (3, "المدير المالي", (decimal?)null) })
        {
            var level = new ApprovalLevel { TenantId = tenantId, Sequence = sequence, NameAr = name, AuthorityLimit = limit, SlaHours = 12 };
            level.Assignments.Add(new ApprovalAssignment { TenantId = tenantId, UserId = requesterId }); policy.Levels.Add(level);
        }
        var order = new Order { TenantId = tenantId, Number = "ORD-APPROVAL", UserId = requesterId, BranchId = branch.Id,
            BranchName = branch.Name, DeliveryAddress = "القاهرة", ReceiverName = "المستلم", ReceiverPhone = "01010000003",
            RequiredDate = DateTime.UtcNow.AddDays(2), ShippingMethod = ShippingMethod.Standard, PaymentMethod = PaymentMethod.CreditLine,
            CostCenterId = center.Id, CostCenterCode = center.Code, CostCenterName = center.NameAr, RequestingDepartment = "المشتريات",
            Status = OrderStatus.PendingApproval, RequiresApproval = true, Subtotal = 15000, Total = 15000 };
        _db.AddRange(tenant, company, branch, requester, delegated, center, policy, order); await _db.SaveChangesAsync();
        var environment = new TestEnvironment(_root);
        var custom = new CustomizationService(_db, _tenant, environment);
        var service = new ApprovalService(_db, _tenant, environment, custom);
        await service.CreateForOrderAsync(order, true, requesterId); await _db.SaveChangesAsync();

        var request = await _db.ApprovalRequests.SingleAsync();
        Assert.Equal(3, request.Steps.Count); Assert.Single(await service.InboxAsync(requesterId, "Pending"));
        var change = await service.RequestChangesAsync(requesterId, request.Id, "إضافة مرفق الميزانية");
        Assert.Equal("ChangesRequested", change.Status);
        var resubmitted = await service.ResubmitAsync(requesterId, request.Id, "تم إرفاق المستند");
        Assert.Equal("Pending", resubmitted.Status);

        var bytes = new byte[] { 37, 80, 68, 70 };
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "budget.pdf")
        { Headers = new HeaderDictionary(), ContentType = "application/pdf" };
        var attachment = await service.UploadAsync(requesterId, request.Id, file);
        Assert.True(File.Exists((await service.FileAsync(requesterId, attachment.Id)).Path));

        var delegatedDetail = await service.DelegateAsync(requesterId, request.Id, new ApprovalDelegateDto(delegateId, "خارج المكتب"));
        Assert.True(delegatedDetail.CanAct);
        await service.ApproveAsync(delegateId, request.Id, "موافق من مدير القسم");
        await service.ApproveAsync(requesterId, request.Id, "الميزانية محجوزة");
        var final = await service.ApproveAsync(requesterId, request.Id, "اعتماد مالي نهائي");
        Assert.Equal("Approved", final.Status);
        var savedOrder = await _db.Orders.SingleAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Confirmed, savedOrder.Status);
        var savedCenter = await _db.CostCenters.SingleAsync(c => c.Id == center.Id);
        Assert.Equal(0, savedCenter.ReservedAmount); Assert.Equal(order.Total, savedCenter.UsedAmount);
        Assert.True(await _db.Notifications.CountAsync() >= 4);

        _tenant.TenantId = Guid.NewGuid();
        var hidden = await Assert.ThrowsAsync<ApiException>(() => service.DetailAsync(requesterId, request.Id));
        Assert.Equal(404, hidden.StatusCode);
    }

    public void Dispose()
    {
        _db.Dispose(); _connection.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
