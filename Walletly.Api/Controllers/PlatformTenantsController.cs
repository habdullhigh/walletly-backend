using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Walletly.Api.Authorization;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;

namespace Walletly.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
public class PlatformTenantsController(ITenantService tenantService) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PlatformTenantsWrite)]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantService.CreateAsync(request, cancellationToken);
        return result.IsFailure
            ? ToProblem(result.Error)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
