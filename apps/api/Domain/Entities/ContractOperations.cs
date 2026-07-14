using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class CompanyContractProduct : TenantEntity
{
    public Guid ContractId { get; set; }
    public CompanyContract Contract { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal ContractPrice { get; set; }
    public int EstimatedAnnualQuantity { get; set; }
}

public class CompanyContractQuantityTier : TenantEntity
{
    public Guid ContractId { get; set; }
    public CompanyContract Contract { get; set; } = null!;
    public int MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public decimal AdditionalDiscountPercent { get; set; }
}

public enum ContractAttachmentType { Draft, SignedContract, PriceAnnex, Other }
public class CompanyContractAttachment : TenantEntity
{
    public Guid ContractId { get; set; }
    public CompanyContract Contract { get; set; } = null!;
    public ContractAttachmentType Type { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
}

public enum ContractApprovalStatus { Pending, Approved, Rejected }
public class CompanyContractApproval : TenantEntity
{
    public Guid ContractId { get; set; }
    public CompanyContract Contract { get; set; } = null!;
    public int Sequence { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string LabelAr { get; set; } = string.Empty;
    public Guid? AssignedUserId { get; set; }
    public ContractApprovalStatus Status { get; set; } = ContractApprovalStatus.Pending;
    public Guid? DecidedByUserId { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
}

public enum ContractPriceRevisionStatus { PendingCustomerApproval, Scheduled, Applied, Rejected }
public class ContractPriceRevision : TenantEntity
{
    public Guid ContractId { get; set; }
    public CompanyContract Contract { get; set; } = null!;
    public DateTime EffectiveAt { get; set; }
    public DateTime? CustomerApprovedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public ContractPriceRevisionStatus Status { get; set; } = ContractPriceRevisionStatus.PendingCustomerApproval;
    public ICollection<ContractPriceRevisionItem> Items { get; set; } = [];
}

public class ContractPriceRevisionItem : TenantEntity
{
    public Guid RevisionId { get; set; }
    public ContractPriceRevision Revision { get; set; } = null!;
    public Guid ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string? Reason { get; set; }
}
