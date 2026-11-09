using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;
using Walletly.Api.Authorization;
using Walletly.Api.Responses;
using Walletly.Application.DTOs.Onboarding;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;

namespace Walletly.Api.Controllers;

[ApiController]
[Route("api/tenant/auth")]
public class TenantAuthController(ITenantAuthService tenantAuthService, ITenantOnboardingService onboardingService) : ApiControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<TenantAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login([FromBody] LoginTenantUserRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantAuthService.LoginAsync(request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    // Public self-service signup. The code is emailed and the response includes a reference for verify-otp.
    [HttpPost("register")]
    [EndpointSummary("Self-service signup")]
    [EndpointDescription("Emails a 6-digit code valid for 10 minutes and returns a twoFactorToken for POST /api/tenant/auth/verify-otp. Returns 409 while a code is still valid, so a new signup cannot reset the attempt counter.")]
    [ProducesResponseType(typeof(ApiResponse<TenantRegistrationResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.RegisterAsync(request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Accepted(ApiResponse.Success(result.Value));
    }

    [HttpPost("resend-otp")]
    [EndpointSummary("Resend the signup code")]
    [EndpointDescription("Resends the current self-service signup code without changing it or extending its expiry. Supply the twoFactorToken returned by signup, or the registration email address.")]
    [ProducesResponseType(typeof(ApiResponse<TenantRegistrationResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.ResendOtpAsync(request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Accepted(ApiResponse.Success(result.Value));
    }

    [HttpPost("verify-otp")]
    [EndpointSummary("Verify the signup code")]
    [EndpointDescription("Returns an access token. The tenant starts as PendingApproval (sandbox only) until a platform admin activates it. Five wrong attempts lock the code. 'password' is required only when the request came from a waitlist approval, because no password was collected at that point.")]
    [ProducesResponseType(typeof(ApiResponse<TenantAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.VerifyOtpAsync(request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpPost("accept-invite")]
    [EndpointSummary("Accept an invitation and set a password")]
    [EndpointDescription("Single use. Returns an access token and makes the tenant Active at once, because the invitation was issued by the Walletly team. Returns 400 when the invitation has expired and 404 when the token is unknown or already used.")]
    [ProducesResponseType(typeof(ApiResponse<TenantAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.AcceptInvitationAsync(request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.TenantUser)]
    [ProducesResponseType(typeof(ApiResponse<TenantUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            return ToErrorResponse(Error.Unauthorized("Auth.InvalidIdentity", "Invalid user identity token."));
        }

        var result = await tenantAuthService.GetProfileAsync(userId, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Ok(ApiResponse.Success(result.Value));
    }
}
