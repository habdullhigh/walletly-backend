using System.Security.Cryptography;
using System.Globalization;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Application.Common;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.DTOs.Onboarding;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Common;
using Walletly.Domain.Entities;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

// Mail is queued before a staging row is saved. If queuing fails, nothing is saved and the person can retry.
// A failed OTP attempt is saved, otherwise the attempt cap never increases.
public class TenantOnboardingService(
    ITemporaryTenantRepository temporaryTenantRepository,
    ITenantRepository tenantRepository,
    ITenantAccountOwnerRepository accountOwnerRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IWebhookSecretProtector webhookSecretProtector,
    ITenantTokenGenerator tenantTokenGenerator,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider,
    OnboardingOptions options) : ITenantOnboardingService
{
    private const int MinPasswordLength = 8;
    private const string VerificationTemplate = "tenant-verification";
    private const string InvitationTemplate = "tenant-invitation";
    private const string WelcomeTemplate = "tenant-welcome";
    private const string ActivatedTemplate = "tenant-activated";
    private const string InvitedRole = "Owner";

    private string BaseUrl => options.PublicBaseUrl.TrimEnd('/');

    public async Task<Result<TemporaryTenantDto>> JoinWaitlistAsync(WaitlistRequest request, CancellationToken cancellationToken = default)
    {
        var email = EmailAddress.Normalise(request.Email);
        var now = dateTimeProvider.UtcNow;

        if (await accountOwnerRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return TenantUserErrors.EmailTaken;
        }

        var existing = await temporaryTenantRepository.GetOpenByEmailAsync(email, cancellationToken);
        if (existing is not null && BlockingRequestError(existing, now) is { } blocked)
        {
            return blocked;
        }

        var entry = TemporaryTenant.CreateWaitlistEntry(request.TenantName, email, request.FirstName, request.LastName, request.ExpectedMonthlyVolume, request.UseCases, now);
        if (entry.IsFailure)
        {
            return entry.Error;
        }

        if (existing is not null)
        {
            temporaryTenantRepository.Remove(existing);
        }

        temporaryTenantRepository.Add(entry.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(entry.Value);
    }

    public async Task<Result<TemporaryTenantDto>> InviteAsync(AdminInvitationRequest request, string inviterName, CancellationToken cancellationToken = default)
    {
        var email = EmailAddress.Normalise(request.Email);
        var now = dateTimeProvider.UtcNow;

        if (await accountOwnerRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return TenantUserErrors.EmailTaken;
        }

        var existing = await temporaryTenantRepository.GetOpenByEmailAsync(email, cancellationToken);
        if (existing is not null && BlockingRequestError(existing, now) is { } blocked)
        {
            return blocked;
        }

        var invitation = TemporaryTenant.CreateAdminInvitation(request.TenantName, email, request.FirstName, request.LastName, request.AdminNotes, now);
        if (invitation.IsFailure)
        {
            return invitation.Error;
        }

        var sent = await StartInvitationAsync(invitation.Value, inviterName, now, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        if (existing is not null)
        {
            temporaryTenantRepository.Remove(existing);
        }

        temporaryTenantRepository.Add(invitation.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(invitation.Value);
    }

    public async Task<Result<TemporaryTenantDto>> ApproveWaitlistAsync(Guid waitlistEntryId, ApproveWaitlistRequest request, string inviterName, CancellationToken cancellationToken = default)
    {
        var entry = await temporaryTenantRepository.GetByIdAsync(waitlistEntryId, cancellationToken);
        if (entry is null)
        {
            return TemporaryTenantErrors.NotFound;
        }

        if (entry.Status != TemporaryTenantStatus.WaitlistPending)
        {
            return TemporaryTenantErrors.NotWaitlisted;
        }

        var method = (request.Method ?? "invitation").Trim().ToLowerInvariant();
        if (method is not ("invitation" or "otp"))
        {
            return TemporaryTenantErrors.InvalidApprovalMethod;
        }

        var now = dateTimeProvider.UtcNow;
        var sent = method == "invitation"
            ? await StartInvitationAsync(entry, inviterName, now, cancellationToken)
            : await StartOtpAndSendAsync(entry, now, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(entry);
    }

    public async Task<Result<TenantRegistrationResponse>> RegisterAsync(RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsValidPassword(request.Password))
        {
            return TenantUserErrors.WeakPassword;
        }

        if (string.IsNullOrWhiteSpace(request.PasswordConfirmation))
        {
            return TenantUserErrors.PasswordConfirmationRequired;
        }

        if (!string.Equals(request.Password, request.PasswordConfirmation, StringComparison.Ordinal))
        {
            return TenantUserErrors.PasswordMismatch;
        }

        var email = EmailAddress.Normalise(request.Email);
        var now = dateTimeProvider.UtcNow;

        if (await accountOwnerRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return TenantUserErrors.EmailTaken;
        }

        // A live code blocks a new signup. Otherwise a person could reset the attempt counter by signing up again.
        var existing = await temporaryTenantRepository.GetOpenByEmailAsync(email, cancellationToken);
        if (existing is not null && BlockingRequestError(existing, now) is { } blocked)
        {
            return blocked;
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var created = TemporaryTenant.CreateSelfService(request.TenantName, email, request.FirstName, request.LastName, passwordHash, now);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var sent = await StartOtpAndSendAsync(created.Value, now, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        var twoFactorToken = webhookSecretProtector.Protect(created.Value.Id.ToString());

        if (existing is not null)
        {
            temporaryTenantRepository.Remove(existing);
        }

        temporaryTenantRepository.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new TenantRegistrationResponse
        {
            TwoFactorToken = twoFactorToken
        };
    }

    public async Task<Result<TenantRegistrationResponse>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default)
    {
        var temporaryTenant = await FindTemporaryTenantAsync(request, cancellationToken);
        if (temporaryTenant is null || temporaryTenant.Source != TemporaryTenantSource.SelfService)
        {
            return TemporaryTenantErrors.NotFound;
        }

        var code = temporaryTenant.GetOtpForResend(dateTimeProvider.UtcNow);
        if (code.IsFailure)
        {
            return code.Error;
        }

        var sent = await SendOtpAsync(temporaryTenant, code.Value, cancellationToken);
        if (sent.IsFailure)
        {
            return sent.Error;
        }

        var twoFactorToken = webhookSecretProtector.Protect(temporaryTenant.Id.ToString());
        return new TenantRegistrationResponse
        {
            TwoFactorToken = twoFactorToken
        };
    }

    public async Task<Result<TenantAuthResponse>> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        var temporaryTenant = await FindTemporaryTenantAsync(request, cancellationToken);
        if (temporaryTenant is null)
        {
            return TemporaryTenantErrors.NotFound;
        }

        // Checked before the code is counted, so a missing password does not use up an attempt.
        if (temporaryTenant.PasswordHash is null && !IsValidPassword(request.Password))
        {
            return TemporaryTenantErrors.PasswordRequired;
        }

        var verified = temporaryTenant.VerifyOtp((request.Code ?? string.Empty).Trim(), dateTimeProvider.UtcNow);
        if (verified.IsFailure)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return verified.Error;
        }

        var passwordHash = temporaryTenant.PasswordHash ?? passwordHasher.HashPassword(request.Password ?? string.Empty);

        // A verified code gives a sandbox account. Live mode stays locked until a platform admin activates the tenant.
        return await GraduateAsync(temporaryTenant, passwordHash, TenantStatus.PendingApproval, cancellationToken);
    }

    private async Task<TemporaryTenant?> FindTemporaryTenantAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TwoFactorToken))
        {
            try
            {
                var idText = webhookSecretProtector.Unprotect(request.TwoFactorToken);
                if (Guid.TryParse(idText, out var temporaryTenantId))
                {
                    var temporaryTenant = await temporaryTenantRepository.GetByIdAsync(temporaryTenantId, cancellationToken);
                    if (temporaryTenant?.IsOpen == true)
                    {
                        return temporaryTenant;
                    }
                }
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            return null;
        }

        var email = EmailAddress.Normalise(request.Email);
        return await temporaryTenantRepository.GetOpenByEmailAsync(email, cancellationToken);
    }

    private async Task<TemporaryTenant?> FindTemporaryTenantAsync(ResendOtpRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TwoFactorToken))
        {
            try
            {
                var idText = webhookSecretProtector.Unprotect(request.TwoFactorToken);
                if (Guid.TryParse(idText, out var temporaryTenantId))
                {
                    var temporaryTenant = await temporaryTenantRepository.GetByIdAsync(temporaryTenantId, cancellationToken);
                    if (temporaryTenant?.IsOpen == true)
                    {
                        return temporaryTenant;
                    }
                }
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return null;
        }

        return await temporaryTenantRepository.GetOpenByEmailAsync(
            EmailAddress.Normalise(request.Email),
            cancellationToken);
    }

    public async Task<Result<TenantAuthResponse>> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsValidPassword(request.Password))
        {
            return TenantUserErrors.WeakPassword;
        }

        var temporaryTenant = await temporaryTenantRepository.GetByInvitationTokenHashAsync(
            TemporaryTenant.HashInvitationToken(request.Token ?? string.Empty),
            cancellationToken);

        if (temporaryTenant is null)
        {
            return TemporaryTenantErrors.InvitationNotFound;
        }

        var usable = temporaryTenant.EnsureInvitationUsable(dateTimeProvider.UtcNow);
        if (usable.IsFailure)
        {
            return usable.Error;
        }

        // An invitation is issued by the platform team, which has already vetted the tenant, so it goes live at once.
        return await GraduateAsync(temporaryTenant, passwordHasher.HashPassword(request.Password), TenantStatus.Active, cancellationToken);
    }

    public async Task<Result> ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound;
        }

        var activated = tenant.Activate();
        if (activated.IsFailure)
        {
            return activated;
        }

        // Activation opens Live mode. Tenants created before this change already have a Live row, so only add a missing one.
        if (await tenantRepository.GetEnvironmentAsync(tenantId, TenantMode.Live, cancellationToken) is null)
        {
            tenantRepository.AddEnvironment(CreateEnvironment(tenantId, TenantMode.Live));
        }

        var owner = await accountOwnerRepository.GetOwnerAsync(tenantId, cancellationToken);
        if (owner is not null)
        {
            var sent = await SendAsync(ActivatedTemplate, owner.Email, owner.FullName, new Dictionary<string, string>
            {
                ["first_name"] = owner.FirstName,
                ["tenant_name"] = tenant.Name,
                ["dashboard_url"] = $"{BaseUrl}/dashboard"
            }, cancellationToken);

            if (sent.IsFailure)
            {
                return sent;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // Turns a verified staging row into a permanent Tenant, its environments and its owner user, all in one save.
    private async Task<Result<TenantAuthResponse>> GraduateAsync(TemporaryTenant temporaryTenant, string passwordHash, TenantStatus status, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;

        if (await accountOwnerRepository.GetByEmailAsync(temporaryTenant.Email, cancellationToken) is not null)
        {
            return TenantUserErrors.EmailTaken;
        }

        var tenantResult = status == TenantStatus.Active
            ? Tenant.Create(temporaryTenant.TenantName, now)
            : Tenant.CreatePendingApproval(temporaryTenant.TenantName, now);

        if (tenantResult.IsFailure)
        {
            return tenantResult.Error;
        }

        var tenant = tenantResult.Value;

        var ownerResult = TenantAccountOwner.Create(
            tenant.Id,
            temporaryTenant.Email,
            passwordHash,
            temporaryTenant.FirstName,
            temporaryTenant.LastName,
            TenantTeamRole.Admin,
            now);

        if (ownerResult.IsFailure)
        {
            return ownerResult.Error;
        }

        var approved = temporaryTenant.MarkApproved(tenant.Id, now);
        if (approved.IsFailure)
        {
            return approved.Error;
        }

        var welcomed = await SendAsync(WelcomeTemplate, ownerResult.Value.Email, ownerResult.Value.FullName, new Dictionary<string, string>
        {
            ["first_name"] = ownerResult.Value.FirstName,
            ["tenant_name"] = tenant.Name,
            ["dashboard_url"] = $"{BaseUrl}/dashboard",
            ["support_email"] = options.SupportEmail
        }, cancellationToken);

        if (welcomed.IsFailure)
        {
            return welcomed.Error;
        }

        tenantRepository.Add(tenant);

        // Every tenant starts with Test, matching TenantService.CreateAsync. Live is added on activation.
        tenantRepository.AddEnvironment(CreateEnvironment(tenant.Id, TenantMode.Test));

        // An invitation makes the tenant Active straight away, so its Live environment is created now.
        if (tenant.IsLiveModeEnabled)
        {
            tenantRepository.AddEnvironment(CreateEnvironment(tenant.Id, TenantMode.Live));
        }

        accountOwnerRepository.Add(ownerResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TenantResponses.ToAuthResponse(ownerResult.Value, tenantTokenGenerator);
    }

    private TenantEnvironment CreateEnvironment(Guid tenantId, TenantMode mode) =>
        TenantEnvironment.Create(tenantId, mode, webhookSecretProtector.Protect(WebhookSecrets.Generate()));

    private async Task<Result> StartInvitationAsync(TemporaryTenant temporaryTenant, string inviterName, DateTime now, CancellationToken cancellationToken)
    {
        // Only the hash is stored. The raw token goes out by email and nowhere else.
        var rawToken = TemporaryTenant.GenerateInvitationToken();
        var started = temporaryTenant.StartInvitation(TemporaryTenant.HashInvitationToken(rawToken), now);
        if (started.IsFailure)
        {
            return started;
        }

        return await SendAsync(InvitationTemplate, temporaryTenant.Email, $"{temporaryTenant.FirstName} {temporaryTenant.LastName}", new Dictionary<string, string>
        {
            ["invitee_name"] = $"{temporaryTenant.FirstName} {temporaryTenant.LastName}",
            ["inviter_name"] = inviterName,
            ["tenant_name"] = temporaryTenant.TenantName,
            ["role"] = InvitedRole,
            ["invite_url"] = $"{BaseUrl}/accept-invite?token={Uri.EscapeDataString(rawToken)}",
            ["expires_in_days"] = TemporaryTenant.InvitationLifetime.TotalDays.ToString(CultureInfo.InvariantCulture)
        }, cancellationToken);
    }

    private async Task<Result> StartOtpAndSendAsync(TemporaryTenant temporaryTenant, DateTime now, CancellationToken cancellationToken)
    {
        var code = TemporaryTenant.GenerateOtpCode();
        var started = temporaryTenant.StartOtpVerification(code, now);
        if (started.IsFailure)
        {
            return started;
        }

        return await SendOtpAsync(temporaryTenant, code, cancellationToken);
    }

    private Task<Result> SendOtpAsync(TemporaryTenant temporaryTenant, string code, CancellationToken cancellationToken) =>
        SendAsync(VerificationTemplate, temporaryTenant.Email, temporaryTenant.FirstName, new Dictionary<string, string>
        {
            ["first_name"] = temporaryTenant.FirstName,
            ["tenant_name"] = temporaryTenant.TenantName,
            ["otp_code"] = code,
            ["expires_in_minutes"] = TemporaryTenant.OtpLifetime.TotalMinutes.ToString(CultureInfo.InvariantCulture)
        }, cancellationToken);

    // A waitlist entry always blocks, because an admin still has to act on it. A challenge blocks only while it is live.
    // Expired rows are not blocking, so the caller replaces them.
    private static Error? BlockingRequestError(TemporaryTenant open, DateTime now)
    {
        if (open.Status == TemporaryTenantStatus.WaitlistPending)
        {
            return TemporaryTenantErrors.OpenRequestExists;
        }

        if (!open.HasLiveChallenge(now))
        {
            return null;
        }

        return open.Source == TemporaryTenantSource.SelfService
            ? TemporaryTenantErrors.VerificationAlreadySent
            : TemporaryTenantErrors.OpenRequestExists;
    }

    private Task<Result> SendAsync(string templateKey, string email, string? name, IReadOnlyDictionary<string, string> tokens, CancellationToken cancellationToken) =>
        emailService.EnqueueAsync(new EmailMessage(templateKey, [new EmailAddressee(email, name)], tokens), cancellationToken);

    private static bool IsValidPassword(string? password) => password is not null && password.Length >= MinPasswordLength;

    private static TemporaryTenantDto ToDto(TemporaryTenant temporaryTenant) => new()
    {
        Id = temporaryTenant.Id,
        TenantName = temporaryTenant.TenantName,
        Email = temporaryTenant.Email,
        FirstName = temporaryTenant.FirstName,
        LastName = temporaryTenant.LastName,
        Source = temporaryTenant.Source.ToString(),
        Status = temporaryTenant.Status.ToString(),
        ExpectedMonthlyVolume = temporaryTenant.ExpectedMonthlyVolume?.ToString(),
        UseCases = temporaryTenant.UseCases.Select(useCase => useCase.ToString()).ToList(),
        CreatedAt = temporaryTenant.CreatedAt
    };
}
