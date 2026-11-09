using Microsoft.AspNetCore.Mvc;
using Walletly.Api.Responses;
using Walletly.Application.DTOs.Onboarding;
using Walletly.Application.Interfaces;

namespace Walletly.Api.Controllers;

// Public waitlist form. Responses do not expose the entry, so the public cannot see approval state or internal ids.
[ApiController]
[Route("api/waitlist")]
public class WaitlistController(ITenantOnboardingService onboardingService) : ApiControllerBase
{
    [HttpPost]
    [EndpointSummary("Join the tenant waitlist")]
    [EndpointDescription("Public form. Returns 202 with a message only, so the entry id and approval state are never exposed. The Walletly team reviews entries and approves them.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> JoinWaitlist([FromBody] WaitlistRequest request, CancellationToken cancellationToken)
    {
        var result = await onboardingService.JoinWaitlistAsync(request, cancellationToken);
        return result.IsFailure ? ToErrorResponse(result.Error) : Accepted(ApiResponse.Success<object?>(null, "Thanks for joining the waitlist. The Walletly team will review your request."));
    }
}
