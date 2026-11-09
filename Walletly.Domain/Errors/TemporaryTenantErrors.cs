using SharedKernel;

namespace Walletly.Domain.Errors;

public static class TemporaryTenantErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "TemporaryTenants.NotFound",
        "The onboarding request does not exist.");

    public static readonly Error OpenRequestExists = Error.Conflict(
        "TemporaryTenants.OpenRequestExists",
        "An onboarding request for this email is already in progress.");

    public static readonly Error VerificationAlreadySent = Error.Conflict(
        "TemporaryTenants.VerificationAlreadySent",
        "A verification code was already sent. Enter it, or wait for it to expire before requesting another.");

    public static readonly Error NotWaitlisted = Error.Conflict(
        "TemporaryTenants.NotWaitlisted",
        "Only waitlist entries that are still pending can be approved.");

    public static readonly Error NotAwaitingVerification = Error.Conflict(
        "TemporaryTenants.NotAwaitingVerification",
        "This request is not waiting for a verification code.");

    public static readonly Error InvalidVerificationCode = Error.Validation(
        "TemporaryTenants.InvalidVerificationCode",
        "The verification code is incorrect.");

    public static readonly Error VerificationCodeExpired = Error.Validation(
        "TemporaryTenants.VerificationCodeExpired",
        "The verification code has expired. Start the signup again to get a new one.");

    public static readonly Error TooManyVerificationAttempts = Error.Forbidden(
        "TemporaryTenants.TooManyVerificationAttempts",
        "Too many incorrect attempts for this verification code.");

    public static readonly Error InvitationNotFound = Error.NotFound(
        "TemporaryTenants.InvitationNotFound",
        "The invitation is invalid or has already been used.");

    public static readonly Error InvitationExpired = Error.Validation(
        "TemporaryTenants.InvitationExpired",
        "The invitation has expired. Ask the Walletly team for a new one.");

    public static readonly Error PasswordRequired = Error.Validation(
        "TemporaryTenants.PasswordRequired",
        "A password of at least 8 characters is required to finish verifying this account.");

    public static readonly Error NoLiveChallengeToResend = Error.Conflict(
        "TemporaryTenants.NoLiveChallengeToResend",
        "There is no active verification code to resend. Please start the signup process again.");

    public static readonly Error InvalidApprovalMethod = Error.Validation(
        "TemporaryTenants.InvalidApprovalMethod",
        "The approval method must be 'invitation' or 'otp'.");

    public static readonly Error InvalidExpectedMonthlyVolume = Error.Validation(
        "TemporaryTenants.InvalidExpectedMonthlyVolume",
        "The expected monthly volume is not one of the allowed options.");

    public static readonly Error InvalidUseCase = Error.Validation(
        "TemporaryTenants.InvalidUseCase",
        "One or more use cases are not recognised.");

    public static readonly Error UseCasesRequired = Error.Validation(
        "TemporaryTenants.UseCasesRequired",
        "Select at least one use case.");
}
