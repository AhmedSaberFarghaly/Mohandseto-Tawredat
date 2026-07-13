using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mohandseto.Api.Application.AdminContent;
using Mohandseto.Api.Infrastructure;

namespace Mohandseto.Api.Controllers;

[ApiController, Route("api/content"), Authorize]
public sealed class ContentExperienceController(AdminContentService service, ITenantProvider tenantProvider) : ControllerBase
{
    [HttpGet("home")] public Task<HomeExperienceDto> Home(CancellationToken ct) => service.HomeExperienceAsync(tenantProvider.TenantId, ct);
}
