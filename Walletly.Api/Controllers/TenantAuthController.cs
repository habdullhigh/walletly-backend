using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Walletly.Api.Authorization;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;

namespace Walletly.Api.Controllers;

[ApiController]
[Route("api/tenant/auth")]
public class TenantAuthController(ITenantAuthService tenantAuthService) : ApiControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(TenantAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginTenantUserRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantAuthService.LoginAsync(request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.TenantUser)]
    [ProducesResponseType(typeof(TenantUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user identity token." });
        }

        var result = await tenantAuthService.GetProfileAsync(userId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }
}
