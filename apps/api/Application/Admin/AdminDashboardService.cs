using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.Admin;

public sealed class AdminDashboardService(AppDbContext db)
{
    private static readonly RfqStatus[] PendingQuoteStatuses =
    [
        RfqStatus.Submitted,
        RfqStatus.UnderReview,
        RfqStatus.ClarificationRequested,
        RfqStatus.Quoted,
        RfqStatus.Negotiating,
    ];

    public async Task<AdminDashboardDto> GetAsync(int days = 7, CancellationToken ct = default)
    {
        days = days is 7 or 30 or 90 ? days : 7;
        var end = DateTime.UtcNow;
        var start = end.Date.AddDays(-(days - 1));
        var previousStart = start.AddDays(-days);

        var current = await db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end)
            .Select(o => new OrderSnapshot(o.Id, o.TenantId, o.Number, o.CreatedAt, o.Total, o.Status))
            .ToListAsync(ct);
        var previous = await db.Orders.AsNoTracking()
            .Where(o => o.CreatedAt >= previousStart && o.CreatedAt < start)
            .Select(o => new { o.Total, o.Status })
            .ToListAsync(ct);
        var quotes = await db.Rfqs.AsNoTracking()
            .Where(q => q.CreatedAt >= start && q.CreatedAt <= end)
            .Select(q => new { q.Status, q.CreatedAt })
            .ToListAsync(ct);
        var previousPendingQuotes = await db.Rfqs.AsNoTracking()
            .CountAsync(q => q.CreatedAt >= previousStart && q.CreatedAt < start &&
                             PendingQuoteStatuses.Contains(q.Status), ct);

        var tenantIds = current.Select(o => o.TenantId).Distinct().ToList();
        var companies = await db.Companies.AsNoTracking()
            .Where(c => tenantIds.Contains(c.TenantId))
            .ToDictionaryAsync(c => c.TenantId, c => c.LegalName, ct);

        var currentSales = current.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);
        var previousSales = previous.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total);
        var pendingQuotes = quotes.Count(q => PendingQuoteStatuses.Contains(q.Status));
        var activeCompanies = await db.Tenants.AsNoTracking().CountAsync(t => t.Status == TenantStatus.Active, ct);

        var trend = Enumerable.Range(0, days).Select(offset =>
        {
            var date = start.AddDays(offset).Date;
            var daily = current.Where(o => o.CreatedAt.Date == date && o.Status != OrderStatus.Cancelled).ToList();
            return new AdminSalesPointDto(date, daily.Sum(o => o.Total), daily.Count);
        }).ToList();

        var orderStatuses = current.GroupBy(o => o.Status)
            .OrderByDescending(g => g.Count())
            .Select(g => new AdminStatusSliceDto(g.Key.ToString(), g.Count())).ToList();
        var quoteStatuses = quotes.GroupBy(q => q.Status)
            .OrderByDescending(g => g.Count())
            .Select(g => new AdminStatusSliceDto(g.Key.ToString(), g.Count())).ToList();
        var topCompanies = current.Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.TenantId)
            .Select(g => new AdminCompanyPerformanceDto(
                g.Key,
                companies.GetValueOrDefault(g.Key, "شركة غير معروفة"),
                g.Sum(o => o.Total),
                g.Count()))
            .OrderByDescending(x => x.Sales).Take(8).ToList();
        var recent = current.OrderByDescending(o => o.CreatedAt).Take(8)
            .Select(o => new AdminRecentOrderDto(
                o.Id,
                o.Number,
                companies.GetValueOrDefault(o.TenantId, "شركة غير معروفة"),
                o.CreatedAt,
                o.Total,
                o.Status.ToString()))
            .ToList();

        return new AdminDashboardDto(
            DateTime.UtcNow,
            days,
            new AdminDashboardSummaryDto(
                new(currentSales, Change(currentSales, previousSales)),
                new(current.Count, Change(current.Count, previous.Count)),
                new(pendingQuotes, Change(pendingQuotes, previousPendingQuotes)),
                new(activeCompanies, 0)),
            trend,
            orderStatuses,
            quoteStatuses,
            topCompanies,
            recent);
    }

    private static decimal Change(decimal current, decimal previous) =>
        previous == 0 ? current == 0 ? 0 : 100 : Math.Round((current - previous) / previous * 100, 1);

    private sealed record OrderSnapshot(
        Guid Id,
        Guid TenantId,
        string Number,
        DateTime CreatedAt,
        decimal Total,
        OrderStatus Status);
}
