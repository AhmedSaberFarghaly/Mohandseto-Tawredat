namespace Mohandseto.Api.Application.AdminReports;

public sealed record ReportCatalogItemDto(string Code, string TitleAr, string TitleEn, string Category, string Icon, string DescriptionAr);
public sealed record ReportCategoryDto(string Code, string NameAr, string Icon, int Count, IReadOnlyList<ReportCatalogItemDto> Reports);
public sealed record ReportFieldDto(string Code, string LabelAr, string Type);
public sealed record ReportSourceDto(string Code, string NameAr, IReadOnlyList<ReportFieldDto> Fields);
public sealed record SavedReportDto(Guid Id, string Name, string Source, IReadOnlyList<string> Fields, ReportFilterDto Filters,
    string? GroupBy, string ChartType, bool IsFavorite, string ScheduleFrequency, int? ScheduleDay, string? ScheduleTime,
    IReadOnlyList<string> Formats, IReadOnlyList<string> Recipients, bool IsScheduleActive, DateTime? NextRunAt, DateTime? LastRunAt);
public sealed record ReportRunDto(Guid Id, Guid? SavedReportId, string ReportCode, string Status, int RowCount,
    string Formats, string? Recipients, DateTime StartedAt, DateTime? CompletedAt, string? Error);
public sealed record ReportsDashboardDto(IReadOnlyList<ReportCategoryDto> Categories, IReadOnlyList<ReportSourceDto> Sources,
    IReadOnlyList<SavedReportDto> SavedReports, IReadOnlyList<ReportRunDto> RecentRuns,
    IReadOnlyList<ReportOptionDto> Companies, IReadOnlyList<ReportOptionDto> Warehouses, IReadOnlyList<ReportOptionDto> Users);
public sealed record ReportOptionDto(Guid Id, string Name);
public sealed record ReportKpiDto(string Label, decimal Value, string Format, decimal? Change = null);
public sealed record ReportPointDto(string Label, decimal Value);
public sealed record ReportBreakdownDto(string Label, decimal Value, decimal Percent);
public sealed record ReportResultDto(string Code, string TitleAr, string TitleEn, DateTime From, DateTime To,
    IReadOnlyList<ReportKpiDto> Kpis, IReadOnlyList<ReportPointDto> Trend, IReadOnlyList<ReportBreakdownDto> Breakdown,
    IReadOnlyList<ReportFieldDto> Columns, IReadOnlyList<IReadOnlyDictionary<string, string>> Rows, int TotalRows);

public sealed record ReportFilterDto(DateTime? From = null, DateTime? To = null, Guid? CompanyId = null,
    Guid? WarehouseId = null, Guid? UserId = null, string? Status = null, decimal? MinValue = null,
    decimal? MaxValue = null, string? Search = null);
public sealed record PreviewCustomReportDto(string Source, IReadOnlyList<string> Fields, ReportFilterDto Filters,
    string? GroupBy, string ChartType = "Line");
public sealed record SaveCustomReportDto(Guid? Id, string Name, string Source, IReadOnlyList<string> Fields,
    ReportFilterDto Filters, string? GroupBy, string ChartType, bool IsFavorite);
public sealed record ScheduleReportDto(string Frequency, int? Day, string Time, IReadOnlyList<string> Formats,
    IReadOnlyList<string> Recipients, bool IsActive);
public sealed record ReportExportOptionsDto(string Code, ReportFilterDto Filters, string? Source = null,
    IReadOnlyList<string>? Fields = null, string? GroupBy = null, bool IncludeCharts = true,
    bool IncludeBranding = true, string Orientation = "Landscape");
