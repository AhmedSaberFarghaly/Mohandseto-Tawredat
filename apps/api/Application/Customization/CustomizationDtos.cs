namespace Mohandseto.Api.Application.Customization;

public sealed record CustomTemplateSummaryDto(Guid Id, Guid ProductId, string Sku, string NameAr, string? ImageUrl,
    decimal StartingUnitPrice, decimal SetupFee, int MinQuantity, int LeadTimeDays);
public sealed record CustomChoiceDto(Guid Id, string Code, string NameAr, string? DescriptionAr, decimal PriceAdjustment, string? Hex = null);
public sealed record CustomTemplateDto(Guid Id, Guid ProductId, string Sku, string NameAr, string? DescriptionAr, string? ImageUrl,
    decimal StartingUnitPrice, decimal SetupFee, int MinQuantity, int LeadTimeDays, IReadOnlyList<CustomChoiceDto> Placements,
    IReadOnlyList<CustomChoiceDto> PrintMethods, IReadOnlyList<CustomChoiceDto> Materials,
    IReadOnlyList<CustomChoiceDto> Colors, IReadOnlyList<CustomChoiceDto> Sizes);

public sealed class CreateCustomRequestForm
{
    public Guid TemplateId { get; set; }
    public int Quantity { get; set; }
    public Guid PlacementId { get; set; }
    public Guid PrintMethodId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid ColorId { get; set; }
    public Guid SizeId { get; set; }
    public string? CustomText { get; set; }
    public decimal PrintWidthCm { get; set; } = 5;
    public decimal PrintHeightCm { get; set; } = 5;
    public int PrintColorCount { get; set; } = 1;
    public bool DesignServiceRequested { get; set; }
    public string? CustomerNote { get; set; }
    public string? Objective { get; set; }
    public string? Audience { get; set; }
    public string? BrandGuidelines { get; set; }
    public string? PreferredColors { get; set; }
    public string? RequiredText { get; set; }
    public DateTime? DesiredDate { get; set; }
    public IFormFile? Logo { get; set; }
    public Guid? ExistingLogoAssetId { get; set; }
    public IFormFile? DesignFile { get; set; }
}

public sealed record CustomRequestListDto(Guid Id, string Number, string ProductName, string Status, int Quantity,
    decimal DisplayTotal, DateTime CreatedAt, int ProgressPercent);
public sealed record CustomAssetDto(Guid Id, string Name, string ContentType, long SizeBytes, bool IsDesignFile, string DownloadUrl);
public sealed record SavedLogoDto(Guid Id, string Name, string ContentType, long SizeBytes, DateTime CreatedAt, string DownloadUrl);
public sealed record DesignBriefDto(string Objective, string? Audience, string? BrandGuidelines, string? PreferredColors,
    string? RequiredText, DateTime? DesiredDate);
public sealed record DesignMockupDto(Guid Id, string Name, string ContentType, bool IsPrimary, string DownloadUrl);
public sealed record DesignVersionDto(Guid Id, int VersionNumber, string Title, string? ChangeSummary, DateTime CreatedAt,
    IReadOnlyList<DesignMockupDto> Mockups);
public sealed record DesignCommentDto(Guid Id, Guid UserId, string Body, DateTime CreatedAt);
public sealed record ProductionStageDto(Guid Id, string Code, string NameAr, string Status, int SortOrder, DateTime? CompletedAt, string? Note);
public sealed record ProductionSampleDto(Guid Id, int VersionNumber, string Name, string ContentType, string Decision,
    string? Note, string? DecisionNote, DateTime CreatedAt, string DownloadUrl);
public sealed record QualityCheckDto(Guid Id, string NameAr, bool Passed, DateTime? CheckedAt, string? Note);
public sealed record ProductionDto(string Number, DateTime? ScheduledStart, DateTime? EstimatedCompletion,
    IReadOnlyList<ProductionStageDto> Stages, IReadOnlyList<ProductionSampleDto> Samples,
    IReadOnlyList<QualityCheckDto> QualityChecks);
public sealed record CustomRequestDto(Guid Id, string Number, Guid TemplateId, string ProductName, string Sku, string Status,
    int Quantity, string Placement, string PrintMethod, string Material, string Color, string Size, string? CustomText,
    decimal PrintWidthCm, decimal PrintHeightCm, int PrintColorCount,
    bool DesignServiceRequested, string? CustomerNote, decimal EstimatedUnitPrice, decimal EstimatedTotal,
    decimal? QuotedTotal, DateTime? QuoteExpiresAt, int EstimatedLeadTimeDays, IReadOnlyList<CustomAssetDto> Assets,
    DesignBriefDto? DesignBrief, IReadOnlyList<DesignVersionDto> Versions, IReadOnlyList<DesignCommentDto> Comments,
    string? LatestDecision, ProductionDto? Production, DateTime CreatedAt);

public sealed record SaveDesignBriefDto(string Objective, string? Audience, string? BrandGuidelines,
    string? PreferredColors, string? RequiredText, DateTime? DesiredDate);
public sealed record AddDesignCommentDto(string Body);
public sealed record DesignDecisionDto(Guid VersionId, string Decision, string? Note);
public sealed record QuoteResponseDto(bool Accept);
public sealed record SetCustomQuoteDto(decimal Total, DateTime ExpiresAt, int LeadTimeDays);
public sealed class PublishDesignForm
{
    public string Title { get; set; } = string.Empty;
    public string? ChangeSummary { get; set; }
    public IFormFile File { get; set; } = null!;
}
public sealed record UpdateProductionStageDto(string Status, string? Note);
public sealed record SampleDecisionDto(string Decision, string? Note);
public sealed record AddQualityCheckDto(string NameAr, bool Passed, string? Note);
public sealed class PublishSampleForm
{
    public string? Note { get; set; }
    public IFormFile File { get; set; } = null!;
}
