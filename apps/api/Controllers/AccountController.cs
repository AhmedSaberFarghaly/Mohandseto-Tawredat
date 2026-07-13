using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Account;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/account")]
public sealed class AccountController(AccountService account) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet] public Task<AccountOverviewDto> Overview(CancellationToken ct) => account.OverviewAsync(UserId, ct);
    [HttpGet("profile")] public Task<ProfileDto> Profile(CancellationToken ct) => account.ProfileAsync(UserId, ct);
    [HttpPut("profile")] public Task<ProfileDto> UpdateProfile(UpdateProfileDto dto, CancellationToken ct) => account.UpdateProfileAsync(UserId, dto, ct);
    [HttpGet("company")] public Task<CompanyDto> Company(CancellationToken ct) => account.CompanyAsync(ct);
    [HttpGet("branches")] public Task<List<BranchDto>> Branches(CancellationToken ct) => account.BranchesAsync(ct);
    [HttpGet("documents")] public Task<List<CompanyDocumentDto>> Documents(CancellationToken ct) => account.DocumentsAsync(ct);
    [HttpGet("brand")] public Task<BrandProfileDto> Brand(CancellationToken ct) => account.BrandAsync(ct);
    [HttpGet("billing")] public Task<BillingProfileDto> Billing(CancellationToken ct) => account.BillingAsync(ct);
    [HttpGet("contracts")] public Task<List<CompanyContractDto>> Contracts(CancellationToken ct) => account.ContractsAsync(ct);
    [HttpPost("contracts/{id:guid}/renewal-requests")] public Task<RenewalRequestDto> Renew(Guid id, CreateRenewalRequestDto dto, CancellationToken ct) => account.RequestRenewalAsync(UserId, id, dto, ct);
}

[ApiController, Authorize(Roles = "company_owner,company_admin"), Route("api/account/admin")]
public sealed class AccountAdminController(AccountService account) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpPut("company")] public Task<CompanyDto> Company(UpdateCompanyDto dto, CancellationToken ct) => account.UpdateCompanyAsync(UserId, dto, ct);
    [HttpPost("branches")] public Task<BranchDto> CreateBranch(UpsertBranchDto dto, CancellationToken ct) => account.CreateBranchAsync(UserId, dto, ct);
    [HttpPut("branches/{id:guid}")] public Task<BranchDto> UpdateBranch(Guid id, UpsertBranchDto dto, CancellationToken ct) => account.UpdateBranchAsync(UserId, id, dto, ct);
    [HttpDelete("branches/{id:guid}")] public async Task<IActionResult> DeleteBranch(Guid id, CancellationToken ct) { await account.DeleteBranchAsync(UserId, id, ct); return NoContent(); }
    [HttpGet("users")] public Task<List<CompanyUserDto>> Users(CancellationToken ct) => account.UsersAsync(ct);
    [HttpPost("users")] public Task<CompanyUserDto> CreateUser(CreateCompanyUserDto dto, CancellationToken ct) => account.CreateUserAsync(UserId, dto, ct);
    [HttpPut("users/{id:guid}")] public Task<CompanyUserDto> UpdateUser(Guid id, UpdateCompanyUserDto dto, CancellationToken ct) => account.UpdateUserAsync(UserId, id, dto, ct);
    [HttpPost("users/{id:guid}/activate")] public Task<CompanyUserDto> Activate(Guid id, CancellationToken ct) => account.SetUserActiveAsync(UserId, id, true, ct);
    [HttpPost("users/{id:guid}/deactivate")] public Task<CompanyUserDto> Deactivate(Guid id, CancellationToken ct) => account.SetUserActiveAsync(UserId, id, false, ct);
    [HttpPost("invites")] public Task<InviteResultDto> Invite(InviteCompanyUserDto dto, CancellationToken ct) => account.InviteAsync(UserId, dto, ct);
    [HttpGet("permissions")] public Task<List<PermissionDto>> Permissions(CancellationToken ct) => account.PermissionsAsync(ct);
    [HttpGet("roles")] public Task<List<CompanyRoleDto>> Roles(CancellationToken ct) => account.RolesAsync(ct);
    [HttpPost("roles")] public Task<CompanyRoleDto> CreateRole(CreateCompanyRoleDto dto, CancellationToken ct) => account.CreateRoleAsync(UserId, dto, ct);
    [HttpPut("roles/{id:guid}/permissions")] public Task<CompanyRoleDto> RolePermissions(Guid id, UpdateRolePermissionsDto dto, CancellationToken ct) => account.UpdateRolePermissionsAsync(UserId, id, dto, ct);
    [HttpGet("approval-policies")] public Task<List<ApprovalPolicyAccountDto>> Policies(CancellationToken ct) => account.ApprovalPoliciesAsync(ct);
    [HttpPut("approval-policies/{id:guid}")] public Task<ApprovalPolicyAccountDto> Policy(Guid id, UpdateApprovalPolicyDto dto, CancellationToken ct) => account.UpdateApprovalPolicyAsync(UserId, id, dto, ct);
    [HttpGet("cost-centers")] public Task<List<CostCenterAccountDto>> CostCenters(CancellationToken ct) => account.CostCentersAsync(ct);
    [HttpPost("cost-centers")] public Task<CostCenterAccountDto> CreateCostCenter(UpsertCostCenterDto dto, CancellationToken ct) => account.CreateCostCenterAsync(UserId, dto, ct);
    [HttpPut("cost-centers/{id:guid}")] public Task<CostCenterAccountDto> UpdateCostCenter(Guid id, UpsertCostCenterDto dto, CancellationToken ct) => account.UpdateCostCenterAsync(UserId, id, dto, ct);
    [HttpGet("audit")] public Task<List<AccountAuditDto>> Audit([FromQuery] Guid? userId, CancellationToken ct) => account.AuditAsync(userId, ct);
    [HttpPut("brand")] public Task<BrandProfileDto> Brand(UpdateBrandProfileDto dto, CancellationToken ct) => account.UpdateBrandAsync(UserId, dto, ct);
    [HttpPost("brand/logo"), RequestSizeLimit(5_243_904)] public Task<BrandProfileDto> Logo(IFormFile file, CancellationToken ct) => account.UploadLogoAsync(UserId, file, ct);
    [HttpPut("billing")] public Task<BillingProfileDto> Billing(UpdateBillingProfileDto dto, CancellationToken ct) => account.UpdateBillingAsync(UserId, dto, ct);
}

[ApiController, AllowAnonymous, Route("api/account/invites")]
public sealed class AccountInvitesController(AccountService account) : ControllerBase
{
    [HttpPost("accept")] public Task<ProfileDto> Accept(AcceptInviteDto dto, CancellationToken ct) => account.AcceptInviteAsync(dto, ct);
}
