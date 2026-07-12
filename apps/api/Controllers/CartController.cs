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
    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct) { await cart.ClearAsync(UserId, ct); return NoContent(); }
}
