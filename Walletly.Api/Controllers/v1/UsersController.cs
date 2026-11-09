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
[Route("api/v1/users")]
[Authorize(Policy = AuthorizationPolicies.TenantApiKey)]
public class UsersController(ITenantUserService userService) : ApiControllerBase
{
    [HttpPost]
    [EndpointSummary("Create end-user")]
    [EndpointDescription("Creates a new end-user within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentUserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateEnvironmentUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await userService.CreateAsync(environmentId, request, cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    [HttpGet]
    [EndpointSummary("List end-users")]
    [EndpointDescription("Lists end-users within the environment bound to the calling API key with pagination.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EnvironmentUserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await userService.ListAsync(environmentId, skip, take, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpGet("{userId:guid}")]
    [EndpointSummary("Get end-user")]
    [EndpointDescription("Gets a specific end-user by ID within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await userService.GetByIdAsync(environmentId, userId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPut("{userId:guid}")]
    [EndpointSummary("Update end-user")]
    [EndpointDescription("Updates an end-user's details within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateEnvironmentUserRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await userService.UpdateAsync(environmentId, userId, request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPut("{userId:guid}/roles")]
    [EndpointSummary("Assign role to end-user")]
    [EndpointDescription("Assigns or changes a custom role for an end-user within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<EnvironmentUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await userService.AssignRoleAsync(environmentId, userId, request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpDelete("{userId:guid}")]
    [EndpointSummary("Deactivate end-user")]
    [EndpointDescription("Deactivates an end-user within the environment bound to the calling API key.")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetEnvironmentId(out var environmentId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidApiKey", "Unable to determine environment from API key."));
        }

        var result = await userService.DeactivateAsync(environmentId, userId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success<object?>(null));
    }

    private bool TryGetEnvironmentId(out Guid environmentId) =>
        Guid.TryParse(User.FindFirstValue("environment_id"), out environmentId);
}
