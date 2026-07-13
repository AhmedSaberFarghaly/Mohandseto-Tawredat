using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum CartStatus { Active, Converted, Abandoned }

public class Cart : TenantEntity
{
    public Guid UserId { get; set; }
    public CartStatus Status { get; set; } = CartStatus.Active;
    public string? CouponCode { get; set; }
    public string? OrderNote { get; set; }
    public string? RequestingDepartment { get; set; }
    public bool AllowSplitDelivery { get; set; }
    public ICollection<CartItem> Items { get; set; } = [];
}

public class CartItem : TenantEntity
{
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? VariantId { get; set; }
    public ProductVariant? Variant { get; set; }
    public int Quantity { get; set; }
    public bool IsSavedForLater { get; set; }
    public string? CustomizationJson { get; set; }
    public Guid? CustomProductRequestId { get; set; }
    public decimal? CustomUnitPrice { get; set; }
    public decimal? CustomLineTotal { get; set; }
}

public enum CheckoutStatus { Draft, Ready, Submitted, Expired }
public enum ShippingMethod { Standard, Express, Pickup }
public enum PaymentMethod { CreditLine, BankTransfer, CashOnDelivery, MonthlyInvoice, Card, Partial }
public enum OrderStatus { PendingApproval, Confirmed, Processing, Cancelled }

public class CheckoutSession : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid? BranchId { get; set; }
    public CompanyBranch? Branch { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? TimeSlot { get; set; }
    public ShippingMethod ShippingMethod { get; set; } = ShippingMethod.Standard;
    public PaymentMethod? PaymentMethod { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? InternalReference { get; set; }
    public Guid? CostCenterId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? RequestingDepartment { get; set; }
    public string? OrderNote { get; set; }
    public bool AllowSplitDelivery { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    public decimal? CreditPortion { get; set; }
    public decimal? CardPortion { get; set; }
    public CheckoutStatus Status { get; set; } = CheckoutStatus.Draft;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(2);
}

public class Order : TenantEntity
{
    public string Number { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverPhone { get; set; } = string.Empty;
    public DateTime RequiredDate { get; set; }
    public string? TimeSlot { get; set; }
    public ShippingMethod ShippingMethod { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public string? InternalReference { get; set; }
    public Guid? CostCenterId { get; set; }
    public string? CostCenterCode { get; set; }
    public string? CostCenterName { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectCode { get; set; }
    public string? ProjectName { get; set; }
    public string? RequestingDepartment { get; set; }
    public string? OrderNote { get; set; }
    public bool AllowSplitDelivery { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    public decimal? CreditPortion { get; set; }
    public decimal? CardPortion { get; set; }
    public OrderStatus Status { get; set; }
    public bool RequiresApproval { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Savings { get; set; }
    public decimal TaxIncluded { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusHistory> History { get; set; } = [];
}

public class OrderItem : TenantEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string? CustomizationJson { get; set; }
}

public class OrderStatusHistory : TenantEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? Note { get; set; }
}
