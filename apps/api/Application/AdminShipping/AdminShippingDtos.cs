namespace Mohandseto.Api.Application.AdminShipping;

public sealed record ShippingKpisDto(int Ready, int Assigned, int OutForDelivery, int DeliveredToday, int FailedToday,
    decimal OnTimePercent, decimal FirstAttemptPercent);
public sealed record ReadyOrderDto(Guid Id, string Number, string Company, string Address, string Receiver,
    string Phone, DateTime RequiredDate, int ItemCount, bool AllowSplit);
public sealed record ShippingItemDto(Guid OrderItemId, string Name, int Quantity);
public sealed record ShippingEventDto(Guid Id, string Status, string Description, string? Location, double? Latitude,
    double? Longitude, DateTime At);
public sealed record ShippingProofDto(Guid Id, string Type, string? Recipient, string? Note, double? Latitude,
    double? Longitude, bool HasFile, DateTime At);
public sealed record ShipmentRowDto(Guid Id, Guid OrderId, string Number, string OrderNumber, string Company,
    string Status, string Address, string Receiver, string Phone, Guid? DriverId, string? Driver, string? DriverPhone,
    double? DriverLatitude, double? DriverLongitude, double? DestinationLatitude, double? DestinationLongitude,
    string? Zone, decimal WeightKg, decimal Cost, int Attempt, DateTime? ScheduledAt, DateTime? Eta,
    DateTime? DeliveredAt, string? FailureReason, int ItemCount, DateTime CreatedAt);
public sealed record ShipmentDetailDto(ShipmentRowDto Shipment, IReadOnlyList<ShippingItemDto> Items,
    IReadOnlyList<ShippingEventDto> Events, IReadOnlyList<ShippingProofDto> Proofs);
public sealed record CourierDto(Guid Id, string Name, string Phone, string? Avatar, int ActiveShipments,
    int Delivered, int Failed, decimal CompletionPercent, decimal OnTimePercent, decimal Rating,
    double? Latitude, double? Longitude);
public sealed record RouteStopDto(Guid Id, Guid ShipmentId, string ShipmentNumber, string Address, int Sequence,
    string Status, DateTime? ScheduledAt, double? Latitude, double? Longitude);
public sealed record ShippingRouteDto(Guid Id, string Code, Guid DriverId, string Driver, DateTime RouteDate,
    string Status, decimal DistanceKm, int EstimatedMinutes, DateTime? StartedAt, DateTime? CompletedAt,
    IReadOnlyList<RouteStopDto> Stops);
public sealed record ShippingZoneDto(Guid Id, string Name, string Governorate, string? Cities, decimal BaseFee,
    decimal FeePerKg, decimal FeePerKm, int EstimatedDays, bool IsActive, int ShipmentCount, decimal Revenue);
public sealed record ShippingDashboardDto(ShippingKpisDto Kpis, IReadOnlyList<ShipmentRowDto> Shipments,
    IReadOnlyList<ReadyOrderDto> ReadyOrders, IReadOnlyList<CourierDto> Couriers,
    IReadOnlyList<ShippingRouteDto> Routes, IReadOnlyList<ShippingZoneDto> Zones);

public sealed record ShippingItemInputDto(Guid OrderItemId, int Quantity);
public sealed record CreateShipmentDto(Guid OrderId, string? Carrier, string? Zone, decimal WeightKg,
    decimal? DeliveryCost, DateTime? ScheduledAt, double? DestinationLatitude, double? DestinationLongitude,
    IReadOnlyList<ShippingItemInputDto>? Items);
public sealed record ShippingPartitionDto(string? Number, decimal WeightKg, IReadOnlyList<ShippingItemInputDto> Items);
public sealed record SplitShippingDto(IReadOnlyList<ShippingPartitionDto> Shipments);
public sealed record AssignCourierDto(Guid DriverId, DateTime? ScheduledAt, DateTime? EstimatedArrival);
public sealed record CreateRouteDto(Guid DriverId, DateTime RouteDate, double? OriginLatitude,
    double? OriginLongitude, IReadOnlyList<Guid> ShipmentIds);
public sealed record ContactCustomerDto(string Channel, string? Note);
public sealed record FailDeliveryDto(string Reason, string? Note, double? Latitude, double? Longitude);
public sealed record RescheduleDeliveryDto(DateTime ScheduledAt, string? Note);
public sealed record SaveShippingZoneDto(string Name, string Governorate, string? Cities, decimal BaseFee,
    decimal FeePerKg, decimal FeePerKm, int EstimatedDays, bool IsActive);

public sealed class ShippingProofForm
{
    public string Type { get; set; } = "Photo";
    public string? RecipientName { get; set; }
    public string? Note { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public IFormFile File { get; set; } = null!;
}
