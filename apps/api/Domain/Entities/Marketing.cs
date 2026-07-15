using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum MarketingCampaignChannel { Push, Email, WhatsApp, InApp }
public enum MarketingAudienceType { AllCompanies, SelectedCompanies, Sector, Behavior }
public enum MarketingScheduleType { Immediate, Scheduled, Optimal }
public enum MarketingCampaignStatus { Draft, Scheduled, Processing, Sent, Cancelled }
public enum MarketingDeliveryStatus { Queued, Delivered, Failed, Bounced }

/// <summary>A platform campaign and its immutable targeting definition.</summary>
public sealed class MarketingCampaign : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MarketingCampaignChannel Channel { get; set; }
    public MarketingAudienceType AudienceType { get; set; }
    public string? Sector { get; set; }
    public int? BehaviorDays { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? CouponCode { get; set; }
    public MarketingScheduleType ScheduleType { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public MarketingCampaignStatus Status { get; set; } = MarketingCampaignStatus.Draft;
    public decimal Cost { get; set; }
    public int RecipientCount { get; set; }
    public ICollection<MarketingCampaignTenant> TargetTenants { get; set; } = [];
    public ICollection<MarketingDelivery> Deliveries { get; set; } = [];
}

public sealed class MarketingCampaignTenant : BaseEntity
{
    public Guid CampaignId { get; set; }
    public MarketingCampaign Campaign { get; set; } = null!;
    public Guid TenantId { get; set; }
}

/// <summary>Per-recipient delivery audit used by operational and performance reports.</summary>
public sealed class MarketingDelivery : BaseEntity
{
    public Guid CampaignId { get; set; }
    public MarketingCampaign Campaign { get; set; } = null!;
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Destination { get; set; } = string.Empty;
    public MarketingDeliveryStatus Status { get; set; } = MarketingDeliveryStatus.Queued;
    public string? ProviderReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }
    public decimal ConversionRevenue { get; set; }
}
