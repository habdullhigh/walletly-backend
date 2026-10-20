using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Walletly.Api.Authorization;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Enums;

namespace Walletly.Api.Controllers;

// The tenant is taken from the token only, never from the route, so a tenant user cannot reach another tenant's settings.
[ApiController]
[Route("api/tenant")]
[Authorize(Policy = AuthorizationPolicies.TenantUser)]
public class TenantController(ITenantService tenantService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.GetAsync(tenantId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.UpdateSettingsAsync(tenantId, request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpPut("environments/{mode}/allowed-urls")]
    [ProducesResponseType(typeof(TenantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAllowedUrls(TenantMode mode, [FromBody] UpdateAllowedUrlsRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.UpdateAllowedUrlsAsync(tenantId, mode, request, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpPost("environments/{mode}/webhook-secret")]
    [ProducesResponseType(typeof(RotatedWebhookSecretDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateWebhookSecret(TenantMode mode, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.RotateWebhookSecretAsync(tenantId, mode, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpGet("environments/{mode}/api-keys")]
    [ProducesResponseType(typeof(List<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListApiKeys(TenantMode mode, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.ListApiKeysAsync(tenantId, mode, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpPost("environments/{mode}/api-keys")]
    [ProducesResponseType(typeof(CreatedApiKeyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateApiKey(TenantMode mode, [FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.CreateApiKeyAsync(tenantId, mode, request, cancellationToken);
        return result.IsFailure
            ? ToProblem(result.Error)
            : StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpDelete("environments/{mode}/api-keys/{apiKeyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeApiKey(TenantMode mode, Guid apiKeyId, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return Unauthorized(new { message = "Invalid tenant identity token." });
        }

        var result = await tenantService.RevokeApiKeyAsync(tenantId, mode, apiKeyId, cancellationToken);
        return result.IsFailure ? ToProblem(result.Error) : NoContent();
    }

    private bool TryGetTenantId(out Guid tenantId) =>
        Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
}
