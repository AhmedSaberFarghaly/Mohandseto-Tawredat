namespace Mohandseto.Api.Application.AdminPrinting;

public sealed record PrintingKpisDto(int ActiveDesigns, int WaitingCustomer, int InProduction, int Late);
public sealed record PrintingRequestRowDto(Guid Id, string Number, string Company, string Product, int Quantity,
    string Status, Guid? DesignerId, string? Designer, DateTime? DueAt, int VersionCount, int ProgressPercent,
    bool IsLate, DateTime CreatedAt);
public sealed record PrintingDesignerDto(Guid Id, string Name, string Specialty, int ActiveWorkload);
public sealed record PrintingTemplateDto(Guid Id, Guid ProductId, string Sku, string Name, string? Description,
    decimal StartingPrice, decimal SetupFee, int MinQuantity, int LeadTimeDays, bool IsActive,
    int Placements, int PrintMethods, int Materials, int Colors, int Sizes);
public sealed record PrintingLogoLibraryDto(Guid Id, Guid RequestId, string Company, string Name, string ContentType,
    long SizeBytes, string QualityStatus, int? QualityScore, DateTime CreatedAt, string DownloadUrl);
public sealed record PrintingDashboardDto(PrintingKpisDto Kpis, IReadOnlyList<PrintingRequestRowDto> Requests,
    IReadOnlyList<PrintingDesignerDto> Designers, IReadOnlyList<PrintingTemplateDto> Templates,
    IReadOnlyList<PrintingLogoLibraryDto> Logos);

public sealed record PrintingBriefDto(string Objective, string? Audience, string? BrandGuidelines,
    string? PreferredColors, string? RequiredText, DateTime? DesiredDate);
public sealed record PrintingAssetDto(Guid Id, string Name, string ContentType, long SizeBytes, bool IsDesignFile,
    string QualityStatus, int? QualityScore, bool IsVector, bool HasTransparentBackground, bool IsCmykReady,
    bool HasSufficientResolution, bool HasSimpleEffects, string? ReviewedBy, DateTime? ReviewedAt,
    string? ReviewNote, string DownloadUrl);
public sealed record PrintingMockupDto(Guid Id, string Name, string ContentType, bool IsPrimary, string DownloadUrl);
public sealed record PrintingVersionDto(Guid Id, int Number, string Title, string? ChangeSummary, string? Designer,
    DateTime CreatedAt, DateTime? SentToCustomerAt, IReadOnlyList<PrintingMockupDto> Mockups);
public sealed record PrintingCommentDto(Guid Id, string Author, string Body, bool IsInternal, DateTime CreatedAt);
public sealed record PrintingApprovalDto(Guid Id, int Version, string Decision, string Customer, string? Note, DateTime CreatedAt);
public sealed record PrintingStageDto(Guid Id, string Code, string Name, string Status, int SortOrder,
    DateTime? StartedAt, DateTime? CompletedAt, string? Note);
public sealed record PrintingSampleDto(Guid Id, int Version, string Name, string Decision, string? Note,
    string? DecisionNote, DateTime CreatedAt, string DownloadUrl);
public sealed record PrintingQualityDto(Guid Id, string Name, bool Passed, string? Inspector, DateTime? CheckedAt, string? Note);
public sealed record PrintingProductionDto(Guid Id, string Number, DateTime? ScheduledStart, DateTime? EstimatedCompletion,
    DateTime? ActualStart, DateTime? ActualCompletion, int ProducedQuantity, int TargetQuantity,
    string? PackagingType, int UnitsPerPackage, int PackageCount, string? DispatchReference,
    IReadOnlyList<PrintingStageDto> Stages, IReadOnlyList<PrintingSampleDto> Samples,
    IReadOnlyList<PrintingQualityDto> QualityChecks);
public sealed record PrintingRequestDetailDto(PrintingRequestRowDto Request, Guid? OrderId, string Placement,
    string PrintMethod, string Material, string Color, string Size, decimal PrintWidthCm, decimal PrintHeightCm,
    int PrintColorCount, string? CustomerNote, PrintingBriefDto? Brief, IReadOnlyList<PrintingAssetDto> Assets,
    IReadOnlyList<PrintingVersionDto> Versions, IReadOnlyList<PrintingCommentDto> Comments,
    IReadOnlyList<PrintingApprovalDto> Approvals, PrintingProductionDto? Production);

public sealed record AssignPrintingDesignerDto(Guid DesignerId, DateTime DueAt, string? Note);
public sealed record ReviewLogoQualityDto(bool Approve, int Score, bool IsVector, bool HasTransparentBackground,
    bool IsCmykReady, bool HasSufficientResolution, bool HasSimpleEffects, string? Note);
public sealed record SendDesignToCustomerDto(Guid VersionId, string? Message);
public sealed record AddInternalPrintingCommentDto(string Body);
public sealed record StartPrintingProductionDto(DateTime? ScheduledStart, DateTime? EstimatedCompletion);
public sealed record UpdatePrintingStageDto(string Status, int ProducedQuantity, string? Note);
public sealed record SavePackagingDto(string PackagingType, int UnitsPerPackage, int PackageCount);
public sealed record MarkPrintingReadyDto(string? DispatchReference);
public sealed record SavePrintingTemplateDto(string Name, string? Description, decimal SetupFee, int MinQuantity,
    int LeadTimeDays, bool IsActive);
