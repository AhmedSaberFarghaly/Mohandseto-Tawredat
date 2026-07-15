using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

/// <summary>A tenant = one registered client company account (B2B).</summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.PendingVerification;
    public Company? Company { get; set; }
}

public enum TenantStatus { PendingVerification, UnderReview, Active, Rejected, Suspended }

public class Company : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string LegalName { get; set; } = string.Empty;
    public string? LegalNameEn { get; set; }
    public string? CommercialRegistrationNo { get; set; }
    public string? TaxCardNo { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? AddressLine { get; set; }
    public string? Industry { get; set; }
    public int EmployeeCountRange { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CreditUsed { get; set; }
    public CompanyClassification Classification { get; set; } = CompanyClassification.Standard;
    public string? Sector { get; set; }
    public string? Activity { get; set; }
    public CompanySize Size { get; set; } = CompanySize.Small;
    public Guid? AssignedSalesRepUserId { get; set; }
    public string? LeadSource { get; set; }
    public CustomerStage CustomerStage { get; set; } = CustomerStage.Lead;
    public ICollection<CompanyBranch> Branches { get; set; } = [];
    public ICollection<CompanyDocument> Documents { get; set; } = [];
}

public enum CompanyClassification { Strategic, KeyAccount, Standard, Prospect }
public enum CompanySize { Micro, Small, Medium, Large, Enterprise }
public enum CustomerStage { Lead, Qualified, Proposal, Negotiation, Active, AtRisk, Dormant, Lost }

public class CompanyBranch : TenantEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Governorate { get; set; }
    public string? City { get; set; }
    public string? AddressLine { get; set; }
    public string? Phone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsMain { get; set; }
}

public enum CompanyDocumentType { CommercialRegistration, TaxCard, AuthorizationLetter, Other }
public enum DocumentReviewStatus { Pending, Approved, Rejected }

public class CompanyDocument : TenantEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public CompanyDocumentType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DocumentReviewStatus ReviewStatus { get; set; } = DocumentReviewStatus.Pending;
    public string? RejectionReason { get; set; }
}

public class User : BaseEntity
{
    /// <summary>Null for platform staff (admin console users).</summary>
    public Guid? TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public bool PhoneVerified { get; set; }
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPlatformStaff { get; set; }
    public string? AvatarPath { get; set; }
    public string PreferredLanguage { get; set; } = "ar";
    public string PreferredTheme { get; set; } = "system";
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorChannel { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public DateTime? SuspendedUntil { get; set; }
    public string? SuspensionReason { get; set; }
    public Guid? DefaultBranchId { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    /// <summary>Maximum single-order value for this user; null delegates to company policy.</summary>
    public decimal? PurchaseLimit { get; set; }
    public ICollection<UserRole> Roles { get; set; } = [];
}

public class Role : BaseEntity
{
    /// <summary>Null = platform-level role; set = tenant-defined role.</summary>
    public Guid? TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public ICollection<RolePermission> Permissions { get; set; } = [];
}

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
}

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class OtpCode : BaseEntity
{
    public string Phone { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int Attempts { get; set; }
    public bool Consumed { get; set; }
}

public enum OtpPurpose { Login, Registration, PasswordReset, PhoneChange, TwoFactor }

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? Device { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public enum UserAccessScopeType { Branch, Warehouse }

/// <summary>Limits a platform user to specific operational branches or warehouses.</summary>
public class UserAccessScope : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public UserAccessScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
}

/// <summary>Authentication attempt audit retained independently from successful refresh-token sessions.</summary>
public class LoginAudit : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Identifier { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Location { get; set; }
}

public class TwoFactorChallenge : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Consumed { get; set; }
}

public class PasswordResetChallenge : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool Consumed { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? DataJson { get; set; }
    public string? Ip { get; set; }
}
