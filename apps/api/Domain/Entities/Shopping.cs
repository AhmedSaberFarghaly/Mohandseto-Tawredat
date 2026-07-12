using Mohandseto.Api.Domain.Common;

namespace Mohandseto.Api.Domain.Entities;

public enum CartStatus { Active, Converted, Abandoned }

public class Cart : TenantEntity
{
    public Guid UserId { get; set; }
    public CartStatus Status { get; set; } = CartStatus.Active;
    public string? CouponCode { get; set; }
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
}
