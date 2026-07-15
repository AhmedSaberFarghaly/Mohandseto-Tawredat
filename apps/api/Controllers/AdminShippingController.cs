using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminShipping;
using Mohandseto.Api.Application.Common;

namespace Mohandseto.Api.Controllers;

[ApiController, Authorize(Roles = "super_admin,operations_manager,delivery_driver"), Route("api/admin/shipping")]
public sealed class AdminShippingController(AdminShippingService shipping) : ControllerBase
{
    private Guid UserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : throw ApiException.Unauthorized();

    [HttpGet] public Task<ShippingDashboardDto> Dashboard(CancellationToken ct) => shipping.DashboardAsync(ct);
    [HttpGet("shipments/{id:guid}")] public Task<ShipmentDetailDto> Detail(Guid id, CancellationToken ct) => shipping.DetailAsync(id, ct);
    [HttpPost("shipments"), Authorize(Roles = "super_admin,operations_manager")] public Task<ShipmentDetailDto> Create(CreateShipmentDto dto, CancellationToken ct) => shipping.CreateAsync(UserId, dto, ct);
    [HttpPost("shipments/{id:guid}/split"), Authorize(Roles = "super_admin,operations_manager")] public Task<IReadOnlyList<ShipmentDetailDto>> Split(Guid id, SplitShippingDto dto, CancellationToken ct) => shipping.SplitAsync(UserId, id, dto, ct);
    [HttpPut("shipments/{id:guid}/courier"), Authorize(Roles = "super_admin,operations_manager")] public async Task<IActionResult> Assign(Guid id, AssignCourierDto dto, CancellationToken ct) { await shipping.AssignAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("routes"), Authorize(Roles = "super_admin,operations_manager")] public Task<ShippingRouteDto> Route(CreateRouteDto dto, CancellationToken ct) => shipping.CreateRouteAsync(UserId, dto, ct);
    [HttpPost("routes/{id:guid}/optimize"), Authorize(Roles = "super_admin,operations_manager")] public Task<ShippingRouteDto> Optimize(Guid id, CancellationToken ct) => shipping.OptimizeRouteAsync(UserId, id, ct);
    [HttpPost("shipments/{id:guid}/start")] public async Task<IActionResult> Start(Guid id, [FromQuery] double? latitude, [FromQuery] double? longitude, CancellationToken ct) { await shipping.StartAsync(UserId, id, latitude, longitude, ct); return NoContent(); }
    [HttpPost("shipments/{id:guid}/contact")] public async Task<IActionResult> Contact(Guid id, ContactCustomerDto dto, CancellationToken ct) { await shipping.ContactAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("shipments/{id:guid}/proofs"), RequestSizeLimit(10 * 1024 * 1024 + 4096)] public Task<ShippingProofDto> Proof(Guid id, [FromForm] ShippingProofForm form, CancellationToken ct) => shipping.AddProofAsync(UserId, id, form, ct);
    [HttpPost("shipments/{id:guid}/confirm")] public async Task<IActionResult> Confirm(Guid id, [FromQuery] string? recipientName, CancellationToken ct) { await shipping.ConfirmAsync(UserId, id, recipientName, ct); return NoContent(); }
    [HttpPost("shipments/{id:guid}/failed")] public async Task<IActionResult> Fail(Guid id, FailDeliveryDto dto, CancellationToken ct) { await shipping.FailAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("shipments/{id:guid}/reschedule")] public async Task<IActionResult> Reschedule(Guid id, RescheduleDeliveryDto dto, CancellationToken ct) { await shipping.RescheduleAsync(UserId, id, dto, ct); return NoContent(); }
    [HttpPost("zones"), Authorize(Roles = "super_admin,operations_manager")] public Task<ShippingZoneDto> CreateZone(SaveShippingZoneDto dto, CancellationToken ct) => shipping.SaveZoneAsync(UserId, null, dto, ct);
    [HttpPut("zones/{id:guid}"), Authorize(Roles = "super_admin,operations_manager")] public Task<ShippingZoneDto> UpdateZone(Guid id, SaveShippingZoneDto dto, CancellationToken ct) => shipping.SaveZoneAsync(UserId, id, dto, ct);
}
