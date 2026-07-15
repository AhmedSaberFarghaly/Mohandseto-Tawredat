using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public class NotificationPreference : TenantEntity
{
    public Guid UserId { get; set; }
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool OrdersEnabled { get; set; } = true;
    public bool ApprovalsEnabled { get; set; } = true;
    public bool QuotesEnabled { get; set; } = true;
    public bool InvoicesEnabled { get; set; } = true;
    public bool PromotionsEnabled { get; set; }
}

public enum SupportTicketType { Order, Delivery, Payment, Invoice, Product, Account, Technical, Other }
public enum SupportTicketPriority { Low, Normal, High, Urgent }
public enum SupportTicketStatus { Open, InProgress, WaitingCustomer, Resolved, Closed }

public class SupportTicket : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public SupportTicketType Type { get; set; }
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;
    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid? OrderId { get; set; }
    public Guid? AssignedStaffUserId { get; set; }
    public DateTime? FirstResponseDueAt { get; set; }
    public DateTime? ResolutionDueAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public Guid? EscalatedBy { get; set; }
    public string? EscalationReason { get; set; }
    public int? Rating { get; set; }
    public string? RatingComment { get; set; }
    public ICollection<SupportMessage> Messages { get; set; } = [];
    public ICollection<SupportAttachment> Attachments { get; set; } = [];
}

public class SupportMessage : TenantEntity
{
    public Guid TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public Guid SenderUserId { get; set; }
    public bool IsStaff { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? ReadAt { get; set; }
}

public class SupportAttachment : TenantEntity
{
    public Guid TicketId { get; set; }
    public SupportTicket Ticket { get; set; } = null!;
    public Guid UploadedByUserId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public enum CallbackRequestStatus { Requested, Scheduled, Completed, Cancelled }
public class CallbackRequest : TenantEntity
{
    public Guid UserId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public DateTime PreferredAt { get; set; }
    public CallbackRequestStatus Status { get; set; } = CallbackRequestStatus.Requested;
}

public class SupportArticle : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string QuestionAr { get; set; } = string.Empty;
    public string AnswerAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

public class ContentPage : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? WhatsAppPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Address { get; set; }
    public bool IsPublished { get; set; } = true;
}

public enum AccountDeletionStatus { Requested, Cancelled, Completed }
public class AccountDeletionRequest : TenantEntity
{
    public Guid UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AccountDeletionStatus Status { get; set; } = AccountDeletionStatus.Requested;
    public DateTime ScheduledFor { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class MobileAppConfig : BaseEntity
{
    public string Platform { get; set; } = "all";
    public string MinimumVersion { get; set; } = "0.2.0";
    public string LatestVersion { get; set; } = "0.2.0";
    public bool MaintenanceEnabled { get; set; }
    public string? MessageAr { get; set; }
    public string? UpdateUrl { get; set; }
}

/// <summary>Configurable block rendered on the customer application's home page.</summary>
public class HomeSection : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SettingsJson { get; set; }
}

/// <summary>Scheduled promotional banner, optionally limited to one company.</summary>
public class HomeBanner : BaseEntity
{
    public string TitleAr { get; set; } = string.Empty;
    public string? SubtitleAr { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public Guid? TargetTenantId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ContentDispatchChannel { AppNotification, InAppMessage }
public enum ContentDispatchStatus { Draft, Scheduled, Sent, Cancelled }

/// <summary>Administrative broadcast definition and immutable delivery audit.</summary>
public class ContentDispatch : BaseEntity
{
    public ContentDispatchChannel Channel { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public Guid? TargetTenantId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public ContentDispatchStatus Status { get; set; } = ContentDispatchStatus.Draft;
    public int RecipientCount { get; set; }
}
