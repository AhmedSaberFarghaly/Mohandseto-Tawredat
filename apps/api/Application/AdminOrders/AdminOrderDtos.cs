using Mohandseto.Api.Application.Orders;

namespace Mohandseto.Api.Application.AdminOrders;

public record AdminOrderSummaryDto(int Total, int NewToday, int Processing, int OutForDelivery, int Late, int Archived);
public record AdminOrderRowDto(Guid Id, string Number, string Company, string Customer, string Status, decimal Total,
    int ItemCount, DateTime CreatedAt, DateTime RequiredDate, string? PurchaseOrderNumber, Guid? AssignedStaffId,
    string? AssignedStaff, bool IsLate, bool IsArchived);
public record AdminOrderPageDto(IReadOnlyList<AdminOrderRowDto> Items, int Total, int Page, int PageSize, AdminOrderSummaryDto Summary);
public record AdminOrderCompanyDto(Guid TenantId, string LegalName, string? TaxNumber, string? Industry, string? Governorate,
    string? Phone, string? Email, decimal CreditLimit, decimal CreditUsed);
public record AdminOrderCustomerDto(Guid Id, string FullName, string Phone, string? Email, string? JobTitle, string? Department);
public record AdminOrderProductDto(Guid ItemId, Guid ProductId, string Sku, string Name, int Quantity, decimal UnitPrice,
    decimal LineTotal, int StockQty, string StockStatus);
public record AdminOrderNoteDto(Guid Id, Guid StaffUserId, string StaffName, string Body, DateTime At);
public record AdminOrderCommunicationDto(Guid Id, string StaffName, string Channel, string Direction, string Subject, string? Body, DateTime At);
public record AdminOrderRefundDto(Guid Id, decimal Amount, string Method, string Reason, string Reference, string Status, DateTime At);
public record AdminOrderInvoiceDto(Guid Id, string Number, string Status, decimal Total, decimal PaidAmount, DateTime IssuedAt, DateTime DueAt);
public record AdminShipmentItemDto(Guid OrderItemId, string Product, int Quantity);
public record AdminShipmentDto(Guid Id, string Number, string Status, string Carrier, DateTime CreatedAt, IReadOnlyList<AdminShipmentItemDto> Items);
public record AdminRecurringDto(Guid Id, string OrderNumber, string Company, string Frequency, int Interval,
    DateTime NextRunAt, DateTime? EndsAt, bool IsActive, decimal EstimatedValue);
public record AdminOrderDetailDto(OrderDetailDto Order, AdminOrderCompanyDto Company, AdminOrderCustomerDto Customer,
    string? AssignedStaff, DateTime? ArchivedAt, IReadOnlyList<AdminOrderProductDto> Products,
    IReadOnlyList<AdminOrderNoteDto> Notes, IReadOnlyList<AdminOrderCommunicationDto> Communications,
    IReadOnlyList<AdminOrderRefundDto> Refunds, AdminOrderInvoiceDto? Invoice, IReadOnlyList<AdminShipmentDto> Shipments);
public record UpdateAdminOrderQuantitiesDto(IReadOnlyList<AdminOrderQuantityDto> Items, string? Reason);
public record AdminOrderQuantityDto(Guid ItemId, int Quantity);
public record SubstituteOrderProductDto(Guid ItemId, Guid ProductId, string? Reason);
public record SplitOrderShipmentsDto(IReadOnlyList<AdminShipmentInputDto> Shipments);
public record AdminShipmentInputDto(string? Number, string? Carrier, IReadOnlyList<AdminShipmentItemInputDto> Items);
public record AdminShipmentItemInputDto(Guid OrderItemId, int Quantity);
public record AssignAdminOrderDto(Guid StaffUserId);
public record AddAdminOrderNoteDto(string Body);
public record AddOrderCommunicationDto(string Channel, string Direction, string Subject, string? Body);
public record AdminCancelOrderDto(string Reason, string? Details, bool ReleaseReservedStock = true);
public record ProcessAdminRefundDto(decimal Amount, string Method, string Reason);
public record UpdateRecurringAdminDto(bool IsActive, DateTime? NextRunAt);
public record PickingItemDto(Guid ItemId, string Sku, string Product, int Quantity, string Shelf, bool Picked);
public record PackingPackageDto(int PackageNumber, IReadOnlyList<AdminShipmentItemDto> Items, decimal EstimatedWeightKg);
public record AdminStaffOptionDto(Guid Id, string FullName, string? JobTitle, string? Department, int ActiveOrders);
