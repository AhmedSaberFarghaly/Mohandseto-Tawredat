using Microsoft.EntityFrameworkCore;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Finance;
using Mohandseto.Api.Domain.Entities;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Application.AdminAccounting;

public sealed class AdminAccountingService(AppDbContext db, FinanceService finance)
{
    public async Task<AccountingDashboardDto> DashboardAsync(CancellationToken ct = default)
    {
        await RefreshOverdueAsync(ct);
        var invoices = await db.Invoices.AsNoTracking().Include(x => x.Order).Include(x => x.Lines).OrderByDescending(x => x.IssuedAt).ToListAsync(ct);
        var payments = await db.InvoicePayments.AsNoTracking().Include(x => x.Invoice).ThenInclude(x => x.Order).OrderByDescending(x => x.CreatedAt).Take(300).ToListAsync(ct);
        var entries = await db.AccountingEntries.AsNoTracking().OrderByDescending(x => x.OccurredAt).Take(300).ToListAsync(ct);
        var periods = await db.FinancialPeriods.AsNoTracking().OrderByDescending(x => x.StartsAt).ToListAsync(ct);
        var tenantIds = invoices.Select(x => x.TenantId).Concat(entries.Where(x => x.TenantId.HasValue).Select(x => x.TenantId!.Value)).Distinct().ToList();
        var companies = await db.Companies.IgnoreQueryFilters().AsNoTracking().Where(x => tenantIds.Contains(x.TenantId) && !x.IsDeleted).ToDictionaryAsync(x => x.TenantId, x => x.LegalName, ct);
        var orderItemIds = invoices.SelectMany(x => x.Lines).Where(x => x.OrderItemId.HasValue).Select(x => x.OrderItemId!.Value).Distinct().ToList();
        var itemProducts = await db.OrderItems.IgnoreQueryFilters().AsNoTracking().Where(x => orderItemIds.Contains(x.Id)).Select(x => new { x.Id, x.ProductId }).ToDictionaryAsync(x => x.Id, x => x.ProductId, ct);
        var productIds = itemProducts.Values.Distinct().ToList();
        var products = await db.Products.IgnoreQueryFilters().AsNoTracking().Where(x => productIds.Contains(x.Id)).Select(x => new { x.Id, x.NameAr, x.CostPrice, x.BasePrice }).ToDictionaryAsync(x => x.Id, ct);
        var active = invoices.Where(x => x.Status is not (InvoiceStatus.Cancelled or InvoiceStatus.Draft)).ToList();
        var revenue = active.Sum(x => x.Total); var collected = active.Sum(x => x.PaidAmount); var receivables = active.Sum(x => Math.Max(0, x.Total - x.PaidAmount));
        var overdue = active.Where(x => x.DueAt < DateTime.UtcNow && x.PaidAmount < x.Total).Sum(x => x.Total - x.PaidAmount);
        var expenses = entries.Where(x => x.Type is AccountingEntryType.Expense or AccountingEntryType.Refund && x.Status == AccountingEntryStatus.Posted).Sum(x => x.Amount);
        var cost = active.SelectMany(x => x.Lines).Sum(line => line.OrderItemId is { } itemId && itemProducts.TryGetValue(itemId, out var productId) && products.TryGetValue(productId, out var product) ? (product.CostPrice > 0 ? product.CostPrice : product.BasePrice) * line.Quantity : 0);
        var profit = revenue - cost - expenses; var margin = revenue == 0 ? 0 : decimal.Round(profit / revenue * 100, 1);
        var aging = active.Where(x => x.Total > x.PaidAmount).GroupBy(x => x.TenantId).Select(g =>
        {
            decimal Bucket(int min, int max) => g.Where(x => (DateTime.UtcNow.Date - x.DueAt.Date).Days >= min && (DateTime.UtcNow.Date - x.DueAt.Date).Days <= max).Sum(x => x.Total - x.PaidAmount);
            var current = g.Where(x => (DateTime.UtcNow.Date - x.DueAt.Date).Days <= 30).Sum(x => x.Total - x.PaidAmount); var a31 = Bucket(31, 60); var a61 = Bucket(61, 90); var a90 = g.Where(x => (DateTime.UtcNow.Date - x.DueAt.Date).Days > 90).Sum(x => x.Total - x.PaidAmount);
            return new AgingRowDto(g.Key, companies.GetValueOrDefault(g.Key, "شركة عميلة"), current, a31, a61, a90, current + a31 + a61 + a90);
        }).OrderByDescending(x => x.Total).ToList();
        var productProfit = active.SelectMany(x => x.Lines).GroupBy(x => x.Sku).Select(g =>
        {
            var product = g.Select(x => x.OrderItemId).Where(x => x.HasValue).Select(x => itemProducts.GetValueOrDefault(x!.Value)).Where(x => x != Guid.Empty).Select(x => products.GetValueOrDefault(x)).FirstOrDefault(x => x is not null);
            var r = g.Sum(x => x.LineTotal); var c = g.Sum(x => product is null ? 0 : (product.CostPrice > 0 ? product.CostPrice : product.BasePrice) * x.Quantity); var p = r - c;
            return new ProfitRowDto(g.Key, product?.NameAr ?? g.First().DescriptionAr, r, c, p, r == 0 ? 0 : decimal.Round(p / r * 100, 1));
        }).OrderByDescending(x => x.Profit).Take(50).ToList();
        var companyProfit = active.GroupBy(x => x.TenantId).Select(g => { var r = g.Sum(x => x.Total); var c = g.SelectMany(x => x.Lines).Sum(line => line.OrderItemId is { } id && itemProducts.TryGetValue(id, out var pid) && products.TryGetValue(pid, out var product) ? (product.CostPrice > 0 ? product.CostPrice : product.BasePrice) * line.Quantity : 0); var p = r - c; return new ProfitRowDto(g.Key.ToString(), companies.GetValueOrDefault(g.Key, "شركة عميلة"), r, c, p, r == 0 ? 0 : decimal.Round(p / r * 100, 1)); }).OrderByDescending(x => x.Profit).ToList();
        var months = Enumerable.Range(0, 6).Select(offset => new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(offset - 5)).Select(start => { var end = start.AddMonths(1); return new MonthlyFinanceDto(start.ToString("yyyy-MM"), active.Where(x => x.IssuedAt >= start && x.IssuedAt < end).Sum(x => x.Total), payments.Where(x => x.Status == InvoicePaymentStatus.Completed && x.VerifiedAt >= start && x.VerifiedAt < end).Sum(x => x.Amount), entries.Where(x => x.Status == AccountingEntryStatus.Posted && x.Type == AccountingEntryType.Expense && x.OccurredAt >= start && x.OccurredAt < end).Sum(x => x.Amount)); }).ToList();
        var invoicedOrders = invoices.Select(x => x.OrderId).ToList();
        var orderOptions = await db.Orders.AsNoTracking().Where(x => !invoicedOrders.Contains(x.Id) && x.Status != OrderStatus.Cancelled).OrderByDescending(x => x.CreatedAt).Take(100).Select(x => new { x.Id, x.Number, x.TenantId, x.Total }).ToListAsync(ct);
        return new(new(revenue, receivables, collected, overdue, expenses, profit, margin), invoices.Select(x => MapInvoice(x, companies)).ToList(), payments.Select(x => new AccountingPaymentDto(x.Id, x.InvoiceId, x.Invoice.Number, companies.GetValueOrDefault(x.TenantId, "شركة عميلة"), x.Amount, x.Method, x.Status.ToString(), x.Reference, x.BankReference, x.CreatedAt, x.VerifiedAt, x.ReceiptStoredPath is not null)).ToList(), aging,
            entries.Select(x => new AccountingEntryDto(x.Id, x.Number, x.Type.ToString(), x.Status.ToString(), x.TenantId is { } tid ? companies.GetValueOrDefault(tid, "شركة عميلة") : "مهندسيتو", x.Category, x.Description, x.Reference, x.Amount, x.TaxAmount, x.OccurredAt)).ToList(), productProfit, companyProfit,
            active.Sum(x => x.Tax) - entries.Where(x => x.Status == AccountingEntryStatus.Posted && x.Type == AccountingEntryType.Expense).Sum(x => x.TaxAmount), periods.Select(x => new FinancialPeriodDto(x.Id, x.Name, x.StartsAt, x.EndsAt, x.Status.ToString(), x.Revenue, x.Collections, x.Expenses, x.SalesTax, x.NetProfit, x.ClosedAt)).ToList(), months, orderOptions.Select(x => new InvoiceOrderOptionDto(x.Id, x.Number, companies.GetValueOrDefault(x.TenantId, "شركة عميلة"), x.Total)).ToList());
    }

    public async Task<AccountingInvoiceDto> CreateInvoiceAsync(CreateAdminInvoiceDto dto, CancellationToken ct = default)
    {
        if (await db.Invoices.AnyAsync(x => x.OrderId == dto.OrderId, ct)) throw ApiException.Conflict("تم إصدار فاتورة لهذا الطلب بالفعل");
        var order = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == dto.OrderId, ct) ?? throw ApiException.NotFound("الطلب غير موجود");
        var invoice = finance.IssueForOrder(order); invoice.DueAt = dto.DueAt ?? DateTime.UtcNow.AddDays(30); invoice.BuyerTaxNumber = Clean(dto.BuyerTaxNumber, 100); await db.SaveChangesAsync(ct);
        var company = await db.Companies.AsNoTracking().Where(x => x.TenantId == invoice.TenantId).Select(x => x.LegalName).FirstOrDefaultAsync(ct) ?? "شركة عميلة"; return MapInvoice(invoice, new Dictionary<Guid, string> { [invoice.TenantId] = company });
    }

    public async Task<AccountingPaymentDto> RecordTransferAsync(RecordBankTransferDto dto, CancellationToken ct = default)
    {
        var invoice = await db.Invoices.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == dto.InvoiceId, ct) ?? throw ApiException.NotFound("الفاتورة غير موجودة");
        if (dto.Amount <= 0 || dto.Amount > invoice.Total - invoice.PaidAmount || string.IsNullOrWhiteSpace(dto.BankReference)) throw ApiException.BadRequest("بيانات التحويل البنكي غير صالحة");
        var payment = new InvoicePayment { TenantId = invoice.TenantId, InvoiceId = invoice.Id, UserId = invoice.UserId, Amount = dto.Amount, Status = InvoicePaymentStatus.PendingVerification, Method = "BankTransfer", BankReference = Clean(dto.BankReference, 100), Reference = Number("PAY") };
        db.InvoicePayments.Add(payment); await db.SaveChangesAsync(ct); var company = await db.Companies.AsNoTracking().Where(x => x.TenantId == invoice.TenantId).Select(x => x.LegalName).FirstOrDefaultAsync(ct) ?? "شركة عميلة"; return new(payment.Id, invoice.Id, invoice.Number, company, payment.Amount, payment.Method, payment.Status.ToString(), payment.Reference, payment.BankReference, payment.CreatedAt, null, false);
    }

    public async Task<AccountingEntryDto> CreateEntryAsync(Guid actorId, SaveAccountingEntryDto dto, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AccountingEntryType>(dto.Type, true, out var type) || dto.Amount <= 0 || dto.TaxAmount < 0 || dto.TaxAmount > dto.Amount || string.IsNullOrWhiteSpace(dto.Description) || dto.OccurredAt > DateTime.UtcNow.AddDays(1)) throw ApiException.BadRequest("بيانات القيد المالي غير صالحة");
        var company = dto.TenantId is { } tenantId ? await db.Companies.AsNoTracking().FirstOrDefaultAsync(x => x.TenantId == tenantId, ct) : null;
        if (dto.TenantId.HasValue && company is null) throw ApiException.BadRequest("الشركة غير موجودة");
        var entry = new AccountingEntry { Number = Number(type == AccountingEntryType.Expense ? "EXP" : "ADJ"), Type = type, TenantId = dto.TenantId, CompanyId = company?.Id, InvoiceId = dto.InvoiceId, ReturnRequestId = dto.ReturnRequestId, Category = Clean(dto.Category, 100) ?? "عام", Description = Clean(dto.Description, 500)!, Reference = Clean(dto.Reference, 100), Amount = dto.Amount, TaxAmount = dto.TaxAmount, OccurredAt = dto.OccurredAt, Status = dto.PostNow ? AccountingEntryStatus.Posted : AccountingEntryStatus.Draft, PostedAt = dto.PostNow ? DateTime.UtcNow : null, PostedBy = dto.PostNow ? actorId : null };
        db.AccountingEntries.Add(entry); await db.SaveChangesAsync(ct); return new(entry.Id, entry.Number, entry.Type.ToString(), entry.Status.ToString(), company?.LegalName ?? "مهندسيتو", entry.Category, entry.Description, entry.Reference, entry.Amount, entry.TaxAmount, entry.OccurredAt);
    }

    public async Task PostEntryAsync(Guid actorId, Guid id, CancellationToken ct = default) { var entry = await db.AccountingEntries.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("القيد غير موجود"); if (entry.Status != AccountingEntryStatus.Draft) throw ApiException.Conflict("القيد ليس مسودة"); entry.Status = AccountingEntryStatus.Posted; entry.PostedAt = DateTime.UtcNow; entry.PostedBy = actorId; await db.SaveChangesAsync(ct); }

    public async Task<FinancialPeriodDto> ClosePeriodAsync(Guid actorId, CloseFinancialPeriodDto dto, CancellationToken ct = default)
    {
        var start = dto.StartsAt.Date; var end = dto.EndsAt.Date.AddDays(1).AddTicks(-1); if (end <= start || end > DateTime.UtcNow.AddDays(1) || !dto.ReportsReviewed) throw ApiException.BadRequest("راجع التقارير وحدد فترة مالية صحيحة");
        if (await db.FinancialPeriods.AnyAsync(x => x.StartsAt == start && x.EndsAt == end, ct)) throw ApiException.Conflict("تم إغلاق هذه الفترة بالفعل");
        if (await db.AccountingEntries.AnyAsync(x => x.OccurredAt >= start && x.OccurredAt <= end && x.Status == AccountingEntryStatus.Draft, ct)) throw ApiException.Conflict("توجد قيود مالية غير مرحلة داخل الفترة");
        if (await db.InvoicePayments.AnyAsync(x => x.CreatedAt >= start && x.CreatedAt <= end && x.Status == InvoicePaymentStatus.PendingVerification, ct)) throw ApiException.Conflict("توجد مدفوعات غير مطابقة داخل الفترة");
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.IssuedAt >= start && x.IssuedAt <= end && x.Status != InvoiceStatus.Cancelled && x.Status != InvoiceStatus.Draft).ToListAsync(ct); var entries = await db.AccountingEntries.AsNoTracking().Where(x => x.OccurredAt >= start && x.OccurredAt <= end && x.Status == AccountingEntryStatus.Posted).ToListAsync(ct); var collections = await db.InvoicePayments.AsNoTracking().Where(x => x.VerifiedAt >= start && x.VerifiedAt <= end && x.Status == InvoicePaymentStatus.Completed).SumAsync(x => x.Amount, ct); var expenses = entries.Where(x => x.Type is AccountingEntryType.Expense or AccountingEntryType.Refund).Sum(x => x.Amount);
        var period = new FinancialPeriod { Name = $"{start:yyyy-MM-dd} — {end:yyyy-MM-dd}", StartsAt = start, EndsAt = end, Status = FinancialPeriodStatus.Closed, Revenue = invoices.Sum(x => x.Total), Collections = collections, Expenses = expenses, SalesTax = invoices.Sum(x => x.Tax) - entries.Where(x => x.Type == AccountingEntryType.Expense).Sum(x => x.TaxAmount), NetProfit = invoices.Sum(x => x.Total) - expenses, ClosedAt = DateTime.UtcNow, ClosedBy = actorId, ClosingNote = Clean(dto.Note, 500) };
        db.FinancialPeriods.Add(period); await db.SaveChangesAsync(ct); return new(period.Id, period.Name, start, end, period.Status.ToString(), period.Revenue, period.Collections, period.Expenses, period.SalesTax, period.NetProfit, period.ClosedAt);
    }

    private async Task RefreshOverdueAsync(CancellationToken ct) { var late = await db.Invoices.Where(x => x.DueAt < DateTime.UtcNow && x.PaidAmount < x.Total && x.Status != InvoiceStatus.Cancelled && x.Status != InvoiceStatus.Draft).ToListAsync(ct); foreach (var x in late) x.Status = InvoiceStatus.Overdue; if (late.Count > 0) await db.SaveChangesAsync(ct); }
    private static AccountingInvoiceDto MapInvoice(Invoice x, IReadOnlyDictionary<Guid, string> companies) => new(x.Id, x.Number, x.OrderId, x.Order.Number, companies.GetValueOrDefault(x.TenantId, "شركة عميلة"), x.TenantId, x.Status.ToString(), x.Type.ToString(), x.IssuedAt, x.DueAt, x.Subtotal, x.Tax, x.Total, x.PaidAmount, Math.Max(0, x.Total - x.PaidAmount), x.SellerTaxNumber, x.BuyerTaxNumber, x.Lines.Select(l => new AccountingInvoiceLineDto(l.Sku, l.DescriptionAr, l.Quantity, l.UnitPrice, l.TaxAmount, l.LineTotal)).ToList());
    private static string Number(string prefix) => $"{prefix}-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var result = value.Trim(); return result.Length <= max ? result : result[..max]; }
}
