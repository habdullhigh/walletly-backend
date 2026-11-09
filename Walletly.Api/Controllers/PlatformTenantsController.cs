using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Walletly.Api.Authorization;
using Walletly.Api.Responses;
using Walletly.Application.DTOs.Onboarding;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;

namespace Walletly.Api.Controllers;

[ApiController]
[Route("api/platform/tenants")]
public class PlatformTenantsController(ITenantService tenantService, ITenantOnboardingService onboardingService) : ApiControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.PlatformTenantsWrite)]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantService.CreateAsync(request, cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    [HttpPost("invitations")]
    [Authorize(Policy = AuthorizationPolicies.PlatformTenantsWrite)]
    [EndpointSummary("Invite a new tenant owner")]
    [EndpointDescription("Stages the tenant and emails a 72-hour invitation link. The owner sets a password through POST /api/tenant/auth/accept-invite, and the tenant becomes Active (live mode allowed) at once. Returns 409 while a live invitation exists for the email.")]
    [ProducesResponseType(typeof(ApiResponse<TemporaryTenantDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Invite([FromBody] AdminInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.InviteAsync(request, InviterName(), cancellationToken);
        return result.IsFailure
            ? ToErrorResponse(result.Error)
            : StatusCode(StatusCodes.Status201Created, ApiResponse.Success(result.Value));
    }

    // The body is optional. Without one, the entry gets an invitation link.
    [HttpPost("waitlist/{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.PlatformTenantsWrite)]
    [EndpointSummary("Approve a waitlist entry")]
    [EndpointDescription("method 'invitation' (default) emails a 72-hour invitation link, and the tenant becomes Active once accepted. method 'otp' emails a 6-digit code instead. The applicant then calls POST /api/tenant/auth/verify-otp with a password, and the tenant starts as PendingApproval. Only WaitlistPending entries can be approved.")]
    [ProducesResponseType(typeof(ApiResponse<TemporaryTenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveWaitlistEntry(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ApproveWaitlistRequest? request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.ApproveWaitlistAsync(id, request ?? new ApproveWaitlistRequest(), InviterName(), cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    // Activation moves the tenant from sandbox-only to live access and emails the owner.
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = AuthorizationPolicies.PlatformTenantsWrite)]
    [EndpointSummary("Activate a tenant for live mode")]
    [EndpointDescription("Moves a PendingApproval tenant to Active, creates its Live environment (with a new webhook secret) so it can create live API keys, and emails the owner. Returns 409 if the tenant is already Active.")]
    [ProducesResponseType(typeof(ApiResponse<ApiResponse<object>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await onboardingService.ActivateTenantAsync(id, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success<object?>(null));
    }

    // Shown to the invitee as the sender. Falls back to the product name if the token has no name claim.
    private string InviterName() =>
        User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "Walletly";
}
