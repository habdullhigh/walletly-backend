using SharedKernel;
using Walletly.Application.DTOs.Onboarding;
using Walletly.Application.DTOs.Tenants;

namespace Walletly.Application.Interfaces;

public interface ITenantOnboardingService
{
    Task<Result<TemporaryTenantDto>> JoinWaitlistAsync(WaitlistRequest request, CancellationToken cancellationToken = default);
    Task<Result<TemporaryTenantDto>> InviteAsync(AdminInvitationRequest request, string inviterName, CancellationToken cancellationToken = default);
    Task<Result<TemporaryTenantDto>> ApproveWaitlistAsync(Guid waitlistEntryId, ApproveWaitlistRequest request, string inviterName, CancellationToken cancellationToken = default);
    Task<Result<TenantRegistrationResponse>> RegisterAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantRegistrationResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantAuthResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantAuthResponse>> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken = default);
    Task<Result> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
