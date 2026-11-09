using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Walletly.Api.Authorization;
using Walletly.Api.Responses;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;

namespace Walletly.Api.Controllers.v1;

[ApiController]
[Route("api/v1/roles")]
[Authorize(Policy = AuthorizationPolicies.TenantApiKey)]
public class RolesController(ITenantRoleService roleService) : ApiControllerBase
{
    [HttpPost]
    [EndpointSummary("Create custom role")]
    [EndpointDescription("Creates a custom role with app-defined permissions within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<TenantRoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await roleService.CreateAsync(environmentId, request, cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    [HttpGet]
    [EndpointSummary("List custom roles")]
    [EndpointDescription("Lists all custom roles for the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantRoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await roleService.ListAsync(environmentId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpGet("permissions")]
    [EndpointSummary("List available system permissions")]
    [EndpointDescription("Returns the list of system-defined permissions (VA creation, transfers, withdrawals) that can be assigned to custom roles.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlySet<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetPermissions()
    {
        var permissions = roleService.GetAvailablePermissions();
        return Ok(ApiResponse.Success(permissions));
    }

    [HttpGet("{roleId:guid}")]
    [EndpointSummary("Get custom role")]
    [EndpointDescription("Gets a specific custom role by ID within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<TenantRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await roleService.GetByIdAsync(environmentId, roleId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPut("{roleId:guid}")]
    [EndpointSummary("Update custom role")]
    [EndpointDescription("Updates a custom role's name, description, and permissions within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<TenantRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid roleId, [FromBody] UpdateTenantRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await roleService.UpdateAsync(environmentId, roleId, request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpDelete("{roleId:guid}")]
    [EndpointSummary("Delete custom role")]
    [EndpointDescription("Deletes a custom role within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid roleId, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await roleService.DeleteAsync(environmentId, roleId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success<object?>(null));
    }

    private bool TryGetEnvironmentId(out Guid environmentId) =>
        Guid.TryParse(User.FindFirstValue("environment_id"), out environmentId);
}
