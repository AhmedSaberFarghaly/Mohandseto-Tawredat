namespace Mohandseto.Api.Application.Admin;

public record AdminMetricDto(decimal Value, decimal ChangePercent);
public record AdminDashboardSummaryDto(
    AdminMetricDto TotalSales,
    AdminMetricDto NewOrders,
    AdminMetricDto PendingQuotes,
    AdminMetricDto ActiveCompanies);
public record AdminSalesPointDto(DateTime Date, decimal Sales, int Orders);
public record AdminStatusSliceDto(string Status, int Count);
public record AdminRecentOrderDto(
    Guid Id,
    string Number,
    string Company,
    DateTime CreatedAt,
    decimal Total,
    string Status);
public record AdminCompanyPerformanceDto(Guid TenantId, string Company, decimal Sales, int Orders);
public record AdminDashboardDto(
    DateTime GeneratedAt,
    int Days,
    AdminDashboardSummaryDto Summary,
    IReadOnlyList<AdminSalesPointDto> SalesTrend,
    IReadOnlyList<AdminStatusSliceDto> OrdersByStatus,
    IReadOnlyList<AdminStatusSliceDto> QuotesByStatus,
    IReadOnlyList<AdminCompanyPerformanceDto> TopCompanies,
    IReadOnlyList<AdminRecentOrderDto> RecentOrders);
