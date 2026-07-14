using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum CompanyInviteStatus { Pending, Accepted, Expired, Cancelled }

public class CompanyInvite : TenantEntity
{
    public Guid CompanyId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid RoleId { get; set; }
    public Guid? BranchId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public CompanyInviteStatus Status { get; set; } = CompanyInviteStatus.Pending;
}

public class CompanyBrandProfile : TenantEntity
{
    public Guid CompanyId { get; set; }
    public string? LogoPath { get; set; }
    public string PrimaryColor { get; set; } = "#11327A";
    public string SecondaryColor { get; set; } = "#F4A024";
    public string? BrandNameAr { get; set; }
    public string? BrandNameEn { get; set; }
}

public class CompanyBillingProfile : TenantEntity
{
    public Guid CompanyId { get; set; }
    public string InvoiceLegalName { get; set; } = string.Empty;
    public string? BillingEmail { get; set; }
    public string? TaxRegistrationNo { get; set; }
    public string? TaxAddress { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
    public bool PurchaseOrderRequired { get; set; }
}

public enum CompanyContractStatus { Draft, Active, Expiring, Expired, Suspended, PendingApproval }
public enum CompanyContractType { SpecialPrices, AnnualPrinting, ComprehensiveSupply }
public enum ContractPricingMode { FixedPerProduct, MarketDiscount }

public class CompanyContract : TenantEntity
{
    public Guid CompanyId { get; set; }
    public string Number { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public CompanyContractStatus Status { get; set; } = CompanyContractStatus.Active;
    public CompanyContractType Type { get; set; } = CompanyContractType.SpecialPrices;
    public ContractPricingMode PricingMode { get; set; } = ContractPricingMode.FixedPerProduct;
    public decimal MarketDiscountPercent { get; set; }
    public decimal AnnualValue { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
    public decimal CreditLimit { get; set; }
    public bool AutoRenew { get; set; }
    public bool RenewalRequiresApproval { get; set; } = true;
    public int ExpiryAlertDays { get; set; } = 30;
    public decimal EarlyPaymentDiscountPercent { get; set; }
    public int EarlyPaymentDays { get; set; }
    public decimal LatePaymentPenaltyPercent { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string DeliveryPriority { get; set; } = "Normal";
    public int DeliveryHours { get; set; } = 48;
    public decimal DeliveryLatePenaltyPercent { get; set; }
    public bool FreeShipping { get; set; }
    public int CreditReviewMonths { get; set; } = 6;
    public DateTime? ActivatedAt { get; set; }
    public Guid? ActivatedByUserId { get; set; }
    public DateTime? CustomerNotifiedAt { get; set; }
    public string? TermsSummary { get; set; }
    public string? DocumentPath { get; set; }
    public ICollection<CompanyContractProduct> Products { get; set; } = [];
    public ICollection<CompanyContractQuantityTier> QuantityTiers { get; set; } = [];
    public ICollection<CompanyContractAttachment> Attachments { get; set; } = [];
    public ICollection<CompanyContractApproval> Approvals { get; set; } = [];
}

public enum ContractRenewalStatus { Submitted, UnderReview, Approved, Rejected }

public class ContractRenewalRequest : TenantEntity
{
    public Guid ContractId { get; set; }
    public CompanyContract Contract { get; set; } = null!;
    public Guid RequestedByUserId { get; set; }
    public int RequestedMonths { get; set; } = 12;
    public string? Note { get; set; }
    public ContractRenewalStatus Status { get; set; } = ContractRenewalStatus.Submitted;
    public DateTime? DecidedAt { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? DecisionNote { get; set; }
}
