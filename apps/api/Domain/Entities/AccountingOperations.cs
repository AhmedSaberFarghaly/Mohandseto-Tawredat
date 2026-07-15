using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum AccountingEntryType { Expense, CreditNote, DebitNote, Refund }
public enum AccountingEntryStatus { Draft, Posted, Voided }
public enum FinancialPeriodStatus { Open, Closed }

public class AccountingEntry : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? ReturnRequestId { get; set; }
    public AccountingEntryType Type { get; set; }
    public AccountingEntryStatus Status { get; set; } = AccountingEntryStatus.Draft;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedBy { get; set; }
}

public class FinancialPeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public FinancialPeriodStatus Status { get; set; } = FinancialPeriodStatus.Open;
    public decimal Revenue { get; set; }
    public decimal Collections { get; set; }
    public decimal Expenses { get; set; }
    public decimal SalesTax { get; set; }
    public decimal NetProfit { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public string? ClosingNote { get; set; }
}

public class SupportSlaPolicy : BaseEntity
{
    public SupportTicketType Type { get; set; }
    public SupportTicketPriority Priority { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupportReplyTemplate : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public SupportTicketType? Type { get; set; }
    public string Body { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public bool IsActive { get; set; } = true;
}
