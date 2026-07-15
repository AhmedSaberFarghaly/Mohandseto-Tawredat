namespace Mohandseto.Api.Application.AdminAccounting;

public sealed record AccountingKpisDto(decimal Revenue, decimal Receivables, decimal Collected, decimal Overdue, decimal Expenses, decimal NetProfit, decimal ProfitMargin);
public sealed record AccountingInvoiceLineDto(string Sku, string Description, int Quantity, decimal UnitPrice, decimal Tax, decimal Total);
public sealed record AccountingInvoiceDto(Guid Id, string Number, Guid OrderId, string OrderNumber, string Company, Guid TenantId, string Status, string Type, DateTime IssuedAt, DateTime DueAt, decimal Subtotal, decimal Tax, decimal Total, decimal Paid, decimal Outstanding, string SellerTaxNumber, string? BuyerTaxNumber, IReadOnlyList<AccountingInvoiceLineDto> Lines);
public sealed record AccountingPaymentDto(Guid Id, Guid InvoiceId, string InvoiceNumber, string Company, decimal Amount, string Method, string Status, string Reference, string? BankReference, DateTime CreatedAt, DateTime? VerifiedAt, bool HasReceipt);
public sealed record AgingRowDto(Guid TenantId, string Company, decimal Current, decimal Days31To60, decimal Days61To90, decimal Over90, decimal Total);
public sealed record AccountingEntryDto(Guid Id, string Number, string Type, string Status, string Company, string Category, string Description, string? Reference, decimal Amount, decimal TaxAmount, DateTime OccurredAt);
public sealed record ProfitRowDto(string Key, string Label, decimal Revenue, decimal Cost, decimal Profit, decimal Margin);
public sealed record FinancialPeriodDto(Guid Id, string Name, DateTime StartsAt, DateTime EndsAt, string Status, decimal Revenue, decimal Collections, decimal Expenses, decimal SalesTax, decimal NetProfit, DateTime? ClosedAt);
public sealed record MonthlyFinanceDto(string Month, decimal Revenue, decimal Collections, decimal Expenses);
public sealed record InvoiceOrderOptionDto(Guid Id, string Number, string Company, decimal Total);
public sealed record AccountingDashboardDto(AccountingKpisDto Kpis, IReadOnlyList<AccountingInvoiceDto> Invoices, IReadOnlyList<AccountingPaymentDto> Payments, IReadOnlyList<AgingRowDto> Aging, IReadOnlyList<AccountingEntryDto> Entries, IReadOnlyList<ProfitRowDto> ProductProfits, IReadOnlyList<ProfitRowDto> CompanyProfits, decimal SalesTax, IReadOnlyList<FinancialPeriodDto> Periods, IReadOnlyList<MonthlyFinanceDto> Monthly, IReadOnlyList<InvoiceOrderOptionDto> OrdersWithoutInvoice);

public sealed record CreateAdminInvoiceDto(Guid OrderId, DateTime? DueAt, string? BuyerTaxNumber);
public sealed record RecordBankTransferDto(Guid InvoiceId, decimal Amount, string BankReference);
public sealed record SaveAccountingEntryDto(string Type, Guid? TenantId, Guid? InvoiceId, Guid? ReturnRequestId, string Category, string Description, string? Reference, decimal Amount, decimal TaxAmount, DateTime OccurredAt, bool PostNow);
public sealed record CloseFinancialPeriodDto(DateTime StartsAt, DateTime EndsAt, bool ReportsReviewed, string? Note);
