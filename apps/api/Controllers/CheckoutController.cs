using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.Common;
using Mohandseto.Api.Application.Shopping;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize, Route("api/checkout")]
public sealed class CheckoutController(CheckoutService checkout) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();
    [HttpGet("options")] public Task<CheckoutOptionsDto> Options(CancellationToken ct) => checkout.OptionsAsync(UserId, ct);
    [HttpPut("delivery")] public Task<CheckoutOptionsDto> Delivery(UpdateDeliveryDto dto, CancellationToken ct) => checkout.DeliveryAsync(UserId, dto, ct);
    [HttpPut("payment")] public Task<CheckoutOptionsDto> Payment(UpdatePaymentDto dto, CancellationToken ct) => checkout.PaymentAsync(UserId, dto, ct);
    [HttpGet("review")] public Task<CheckoutReviewDto> Review(CancellationToken ct) => checkout.ReviewAsync(UserId, ct);
    [HttpPost("submit")] public Task<OrderCreatedDto> Submit(SubmitCheckoutDto dto, CancellationToken ct) => checkout.SubmitAsync(UserId, dto.AcceptTerms, ct);
}
