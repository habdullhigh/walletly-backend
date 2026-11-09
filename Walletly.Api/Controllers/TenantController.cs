using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Walletly.Api.Authorization;
using Walletly.Api.Responses;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Enums;

using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Api.Controllers;

// The tenant is taken from the token only, never from the route, so a tenant user cannot reach another tenant's settings.
[ApiController]
[Route("api/tenant")]
[Authorize(Policy = AuthorizationPolicies.TenantUser)]
public class TenantController(
    ITenantService tenantService,
    ITenantUserService userService,
    ITenantRoleService roleService,
    ITenantRepository tenantRepository) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.GetAsync(tenantId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.UpdateSettingsAsync(tenantId, request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPut("environments/{mode}/allowed-urls")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAllowedUrls(TenantMode mode, [FromBody] UpdateAllowedUrlsRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.UpdateAllowedUrlsAsync(tenantId, mode, request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPost("environments/{mode}/webhook-secret")]
    [ProducesResponseType(typeof(ApiResponse<RotatedWebhookSecretDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RotateWebhookSecret(TenantMode mode, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.RotateWebhookSecretAsync(tenantId, mode, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpGet("environments/{mode}/api-keys")]
    [ProducesResponseType(typeof(ApiResponse<List<ApiKeyDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListApiKeys(TenantMode mode, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.ListApiKeysAsync(tenantId, mode, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPost("environments/{mode}/api-keys")]
    [ProducesResponseType(typeof(ApiResponse<CreatedApiKeyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateApiKey(TenantMode mode, [FromBody] CreateApiKeyRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.CreateApiKeyAsync(tenantId, mode, request, cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    [HttpDelete("environments/{mode}/api-keys/{apiKeyId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApiResponse<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeApiKey(TenantMode mode, Guid apiKeyId, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var result = await tenantService.RevokeApiKeyAsync(tenantId, mode, apiKeyId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success<object?>(null));
    }

    [HttpGet("environments/{mode}/users")]
    [EndpointSummary("List end-users in environment (Dashboard)")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EnvironmentUserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEnvironmentUsers(TenantMode mode, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var env = await tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (env is null)
        {
            return ToErrorResponse(TenantErrors.EnvironmentNotFound);
        }

        var result = await userService.ListAsync(env.Id, skip, take, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPost("environments/{mode}/users")]
    [EndpointSummary("Create end-user in environment (Dashboard)")]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentUserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEnvironmentUser(TenantMode mode, [FromBody] CreateEnvironmentUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var env = await tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (env is null)
        {
            return ToErrorResponse(TenantErrors.EnvironmentNotFound);
        }

        var result = await userService.CreateAsync(env.Id, request, cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    [HttpGet("environments/{mode}/roles")]
    [EndpointSummary("List custom roles in environment (Dashboard)")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantRoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListEnvironmentRoles(TenantMode mode, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var env = await tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (env is null)
        {
            return ToErrorResponse(TenantErrors.EnvironmentNotFound);
        }

        var result = await roleService.ListAsync(env.Id, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPost("environments/{mode}/roles")]
    [EndpointSummary("Create custom role in environment (Dashboard)")]
    [ProducesResponseType(typeof(ApiResponse<TenantRoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEnvironmentRole(TenantMode mode, [FromBody] CreateTenantRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetTenantId(out var tenantId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid tenant identity token."));
        }

        var env = await tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (env is null)
        {
            return ToErrorResponse(TenantErrors.EnvironmentNotFound);
        }

        var result = await roleService.CreateAsync(env.Id, request, cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    private bool TryGetTenantId(out Guid tenantId) =>
        Guid.TryParse(User.FindFirstValue("tenant_id"), out tenantId);
}
