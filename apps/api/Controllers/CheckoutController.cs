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
    [HttpPost("addresses")] public Task<CheckoutOptionsDto> AddAddress(CreateCheckoutAddressDto dto, CancellationToken ct) => checkout.AddAddressAsync(UserId, dto, ct);
    [HttpPut("context")] public Task<CheckoutOptionsDto> Context(UpdateCheckoutContextDto dto, CancellationToken ct) => checkout.ContextAsync(UserId, dto, ct);
    [HttpPut("delivery")] public Task<CheckoutOptionsDto> Delivery(UpdateDeliveryDto dto, CancellationToken ct) => checkout.DeliveryAsync(UserId, dto, ct);
    [HttpPut("payment")] public Task<CheckoutOptionsDto> Payment(UpdatePaymentDto dto, CancellationToken ct) => checkout.PaymentAsync(UserId, dto, ct);
    [HttpPost("attachments/purchase-order"), RequestSizeLimit(10 * 1024 * 1024 + 1024)]
    public Task<CheckoutAttachmentDto> UploadPurchaseOrder(IFormFile file, CancellationToken ct) => checkout.UploadAttachmentAsync(UserId, file, ct);
    [HttpGet("attachments/{id:guid}")]
    public async Task<IActionResult> Attachment(Guid id, CancellationToken ct)
    {
        var file = await checkout.AttachmentAsync(UserId, id, ct);
        return PhysicalFile(file.Path, file.ContentType, file.Name, enableRangeProcessing: true);
    }
    [HttpPost("payment-attempts")] public Task<PaymentAttemptDto> CreatePayment(CreatePaymentAttemptDto dto, CancellationToken ct) => checkout.CreatePaymentAsync(UserId, dto, ct);
    [HttpPost("payment-attempts/{id:guid}/confirm")] public Task<PaymentAttemptDto> ConfirmPayment(Guid id, ConfirmPaymentAttemptDto dto, CancellationToken ct) => checkout.ConfirmPaymentAsync(UserId, id, dto, ct);
    [HttpGet("review")] public Task<CheckoutReviewDto> Review(CancellationToken ct) => checkout.ReviewAsync(UserId, ct);
    [HttpPost("submit")] public Task<OrderCreatedDto> Submit(SubmitCheckoutDto dto, CancellationToken ct) => checkout.SubmitAsync(UserId, dto.AcceptTerms, ct);
}
