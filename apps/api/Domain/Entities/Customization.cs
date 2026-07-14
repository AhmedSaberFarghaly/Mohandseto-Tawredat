using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum CustomRequestStatus
{
    Draft,
    AwaitingQuote,
    Quoted,
    DesignInProgress,
    AwaitingDesignApproval,
    DesignApproved,
    AwaitingCheckout,
    AwaitingOrderApproval,
    AwaitingSampleApproval,
    InProduction,
    QualityCheck,
    Ready,
    Completed,
    Rejected,
    Cancelled,
}

public enum DesignApprovalDecision { Pending, Approved, RevisionRequested, Rejected }
public enum ProductionStageStatus { Pending, InProgress, Completed, Blocked }
public enum SampleApprovalDecision { Pending, Approved, RevisionRequested, Rejected }
public enum LogoQualityStatus { Pending, Approved, Rejected }

public class CustomProductTemplate : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string NameAr { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public decimal SetupFee { get; set; }
    public int MinQuantity { get; set; } = 25;
    public int LeadTimeDays { get; set; } = 7;
    public bool IsActive { get; set; } = true;
    public ICollection<CustomizationOption> Options { get; set; } = [];
    public ICollection<PrintMethod> PrintMethods { get; set; } = [];
    public ICollection<CustomMaterial> Materials { get; set; } = [];
    public ICollection<CustomColor> Colors { get; set; } = [];
    public ICollection<CustomSize> Sizes { get; set; } = [];
}

public class CustomizationOption : BaseEntity
{
    public Guid TemplateId { get; set; }
    public CustomProductTemplate Template { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Type { get; set; } = "placement";
    public decimal PriceAdjustment { get; set; }
    public int SortOrder { get; set; }
}

public class PrintMethod : BaseEntity
{
    public Guid TemplateId { get; set; }
    public CustomProductTemplate Template { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public decimal UnitPriceAdjustment { get; set; }
    public int MinQuantity { get; set; } = 25;
    public int SortOrder { get; set; }
}

public class CustomMaterial : BaseEntity
{
    public Guid TemplateId { get; set; }
    public CustomProductTemplate Template { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal UnitPriceAdjustment { get; set; }
    public int SortOrder { get; set; }
}

public class CustomColor : BaseEntity
{
    public Guid TemplateId { get; set; }
    public CustomProductTemplate Template { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Hex { get; set; } = "#0E2D6D";
    public decimal UnitPriceAdjustment { get; set; }
    public int SortOrder { get; set; }
}

public class CustomSize : BaseEntity
{
    public Guid TemplateId { get; set; }
    public CustomProductTemplate Template { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public decimal UnitPriceAdjustment { get; set; }
    public int SortOrder { get; set; }
}

public class CustomProductRequest : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    public CustomProductTemplate Template { get; set; } = null!;
    public Guid? OrderId { get; set; }
    public CustomRequestStatus Status { get; set; } = CustomRequestStatus.Draft;
    public bool DesignServiceRequested { get; set; }
    public string? CustomerNote { get; set; }
    public decimal EstimatedTotal { get; set; }
    public decimal? QuotedTotal { get; set; }
    public DateTime? QuoteExpiresAt { get; set; }
    public int EstimatedLeadTimeDays { get; set; }
    public Guid? AssignedDesignerId { get; set; }
    public DateTime? DesignDueAt { get; set; }
    public DateTime? DesignSentAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public ICollection<CustomRequestItem> Items { get; set; } = [];
    public ICollection<LogoAsset> LogoAssets { get; set; } = [];
    public DesignBrief? DesignBrief { get; set; }
    public ICollection<DesignVersion> DesignVersions { get; set; } = [];
    public ICollection<DesignComment> Comments { get; set; } = [];
    public ICollection<DesignApproval> Approvals { get; set; } = [];
    public ProductionJob? ProductionJob { get; set; }
}

public class CustomRequestItem : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public int Quantity { get; set; }
    public Guid? OptionId { get; set; }
    public Guid? PrintMethodId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ColorId { get; set; }
    public Guid? SizeId { get; set; }
    public string? CustomText { get; set; }
    public decimal PrintWidthCm { get; set; } = 5;
    public decimal PrintHeightCm { get; set; } = 5;
    public int PrintColorCount { get; set; } = 1;
    public decimal EstimatedUnitPrice { get; set; }
}

public class LogoAsset : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsDesignFile { get; set; }
    public LogoQualityStatus QualityStatus { get; set; } = LogoQualityStatus.Pending;
    public int? QualityScore { get; set; }
    public bool IsVector { get; set; }
    public bool HasTransparentBackground { get; set; }
    public bool IsCmykReady { get; set; }
    public bool HasSufficientResolution { get; set; }
    public bool HasSimpleEffects { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public class DesignBrief : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public string Objective { get; set; } = string.Empty;
    public string? Audience { get; set; }
    public string? BrandGuidelines { get; set; }
    public string? PreferredColors { get; set; }
    public string? RequiredText { get; set; }
    public DateTime? DesiredDate { get; set; }
}

public class DesignVersion : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public Guid? CreatedByDesignerId { get; set; }
    public DateTime? SentToCustomerAt { get; set; }
    public Guid? SentByUserId { get; set; }
    public ICollection<DesignMockup> Mockups { get; set; } = [];
}

public class DesignMockup : TenantEntity
{
    public Guid VersionId { get; set; }
    public DesignVersion Version { get; set; } = null!;
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsPrimary { get; set; }
}

public class DesignComment : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
}

public class DesignApproval : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public Guid VersionId { get; set; }
    public DesignVersion Version { get; set; } = null!;
    public Guid UserId { get; set; }
    public DesignApprovalDecision Decision { get; set; }
    public string? Note { get; set; }
}

public class ProductionJob : TenantEntity
{
    public Guid RequestId { get; set; }
    public CustomProductRequest Request { get; set; } = null!;
    public string Number { get; set; } = string.Empty;
    public DateTime? ScheduledStart { get; set; }
    public DateTime? EstimatedCompletion { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualCompletion { get; set; }
    public int ProducedQuantity { get; set; }
    public string? PackagingType { get; set; }
    public int UnitsPerPackage { get; set; }
    public int PackageCount { get; set; }
    public string? DispatchReference { get; set; }
    public ICollection<ProductionStage> Stages { get; set; } = [];
    public ICollection<ProductionSample> Samples { get; set; } = [];
    public ICollection<QualityCheck> QualityChecks { get; set; } = [];
}

public class ProductionSample : TenantEntity
{
    public Guid ProductionJobId { get; set; }
    public ProductionJob ProductionJob { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Note { get; set; }
    public SampleApprovalDecision Decision { get; set; }
    public Guid? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public string? DecisionNote { get; set; }
}

public class ProductionStage : TenantEntity
{
    public Guid ProductionJobId { get; set; }
    public ProductionJob ProductionJob { get; set; } = null!;
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public ProductionStageStatus Status { get; set; }
    public int SortOrder { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Note { get; set; }
}

public class QualityCheck : TenantEntity
{
    public Guid ProductionJobId { get; set; }
    public ProductionJob ProductionJob { get; set; } = null!;
    public string CheckNameAr { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public Guid? CheckedBy { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string? Note { get; set; }
}
