using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Shopping;

namespace Mohandseto.Api.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public sealed class CartController(CartService cart) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw ApiException.Unauthorized();

    [HttpGet]
    public Task<CartDto> Get(CancellationToken ct) => cart.GetAsync(UserId, ct);
    [HttpPost("items")]
    public Task<CartDto> Add(AddCartItemDto dto, CancellationToken ct) => cart.AddAsync(UserId, dto, ct);
    [HttpPut("items/{itemId:guid}")]
    public Task<CartDto> Update(Guid itemId, UpdateCartItemDto dto, CancellationToken ct) => cart.UpdateAsync(UserId, itemId, dto.Quantity, ct);
    [HttpDelete("items/{itemId:guid}")]
    public Task<CartDto> Remove(Guid itemId, CancellationToken ct) => cart.RemoveAsync(UserId, itemId, ct);
    [HttpPost("items/{itemId:guid}/save-for-later")]
    public Task<CartDto> SaveForLater(Guid itemId, CancellationToken ct) => cart.SetSavedAsync(UserId, itemId, true, ct);
    [HttpPost("items/{itemId:guid}/restore")]
    public Task<CartDto> Restore(Guid itemId, CancellationToken ct) => cart.SetSavedAsync(UserId, itemId, false, ct);
    [HttpPut("items/{itemId:guid}/note")]
    public Task<CartDto> ItemNote(Guid itemId, UpdateCartItemNoteDto dto, CancellationToken ct) => cart.SetItemNoteAsync(UserId, itemId, dto.Note, ct);
    [HttpPut("order-note")]
    public Task<CartDto> OrderNote(UpdateCartItemNoteDto dto, CancellationToken ct) => cart.SetOrderNoteAsync(UserId, dto.Note, ct);
    [HttpPost("coupon")]
    public Task<CartDto> ApplyCoupon(ApplyCouponDto dto, CancellationToken ct) => cart.ApplyCouponAsync(UserId, dto.Code, ct);
    [HttpDelete("coupon")]
    public Task<CartDto> RemoveCoupon(CancellationToken ct) => cart.RemoveCouponAsync(UserId, ct);
    [HttpPost("save")]
    public Task<SavedCartDto> SaveCart(SaveCartDto dto, CancellationToken ct) => cart.SaveCartAsync(UserId, dto.Name, ct);
    [HttpGet("saved")]
    public Task<List<SavedCartDto>> SavedCarts(CancellationToken ct) => cart.SavedCartsAsync(UserId, ct);
    [HttpPost("saved/{id:guid}/restore")]
    public Task<CartDto> RestoreCart(Guid id, CancellationToken ct) => cart.RestoreCartAsync(UserId, id, ct);
    [HttpPost("acknowledge-prices")]
    public Task<CartDto> AcknowledgePrices(CancellationToken ct) => cart.AcknowledgePricesAsync(UserId, ct);
    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct) { await cart.ClearAsync(UserId, ct); return NoContent(); }
}
