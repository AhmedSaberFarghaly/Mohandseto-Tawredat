using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum ReportScheduleFrequency { None, Daily, Weekly, Monthly }
public enum ReportRunStatus { Processing, Completed, Failed }

public class SavedReport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "[]";
    public string FiltersJson { get; set; } = "{}";
    public string? GroupBy { get; set; }
    public string ChartType { get; set; } = "Line";
    public bool IsFavorite { get; set; }
    public Guid OwnerUserId { get; set; }
    public ReportScheduleFrequency ScheduleFrequency { get; set; }
    public int? ScheduleDay { get; set; }
    public TimeOnly? ScheduleTime { get; set; }
    public string FormatsCsv { get; set; } = "Excel";
    public string? RecipientsCsv { get; set; }
    public bool IsScheduleActive { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public ICollection<ReportRun> Runs { get; set; } = [];
}

public class ReportRun : BaseEntity
{
    public Guid? SavedReportId { get; set; }
    public SavedReport? SavedReport { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public ReportRunStatus Status { get; set; } = ReportRunStatus.Processing;
    public int RowCount { get; set; }
    public string FormatsCsv { get; set; } = string.Empty;
    public string? RecipientsCsv { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
}
