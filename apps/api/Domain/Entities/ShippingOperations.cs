using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum DeliveryRouteStatus { Planned, Optimized, InProgress, Completed, Cancelled }
public enum DeliveryStopStatus { Pending, InProgress, Delivered, Failed, Skipped }

public class DeliveryRoute : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid DriverUserId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public DateTime RouteDate { get; set; }
    public DeliveryRouteStatus Status { get; set; } = DeliveryRouteStatus.Planned;
    public double? OriginLatitude { get; set; }
    public double? OriginLongitude { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public int EstimatedMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<DeliveryRouteStop> Stops { get; set; } = [];
}

public class DeliveryRouteStop : BaseEntity
{
    public Guid RouteId { get; set; }
    public DeliveryRoute Route { get; set; } = null!;
    public Guid ShipmentId { get; set; }
    public int Sequence { get; set; }
    public DeliveryStopStatus Status { get; set; } = DeliveryStopStatus.Pending;
    public DateTime? ScheduledAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class DeliveryZone : BaseEntity
{
    public string NameAr { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public string? CitiesCsv { get; set; }
    public decimal BaseFee { get; set; }
    public decimal FeePerKg { get; set; }
    public decimal FeePerKm { get; set; }
    public int EstimatedDays { get; set; } = 2;
    public bool IsActive { get; set; } = true;
}
