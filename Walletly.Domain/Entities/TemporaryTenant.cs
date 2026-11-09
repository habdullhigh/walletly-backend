using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using SharedKernel;
using Walletly.Domain.Common;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

// Staging row for a tenant that has not been provisioned yet. It becomes a Tenant and a TenantUser only after the
// person proves control of the email address (OTP or invitation link). The row stays afterwards as the audit trail.
public sealed class TemporaryTenant : Entity
{
    public const int OtpMaxAttempts = 5;
    public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);

    private const int MaxTenantNameLength = 150;
    private const int MaxNameLength = 100;

    // For EF Core materialisation only.
    private TemporaryTenant()
    {
    }

    private TemporaryTenant(
        Guid id,
        string tenantName,
        string email,
        string firstName,
        string lastName,
        string? passwordHash,
        TemporaryTenantSource source,
        TemporaryTenantStatus status,
        string? adminNotes,
        DateTime createdAtUtc)
        : base(id)
    {
        TenantName = tenantName;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PasswordHash = passwordHash;
        Source = source;
        Status = status;
        AdminNotes = adminNotes;
        CreatedAt = createdAtUtc;
    }

    public string TenantName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    // Set at signup for self-service. Waitlist and admin rows get it later, when the person sets a password.
    public string? PasswordHash { get; private set; }

    public TemporaryTenantSource Source { get; private set; }
    public TemporaryTenantStatus Status { get; private set; }

    // The OTP is stored as issued (a 6-digit code cannot be hashed usefully). OtpAttemptsCount is what limits guessing.
    public string? OtpCode { get; private set; }
    public DateTime? OtpExpiresAt { get; private set; }
    public int OtpAttemptsCount { get; private set; }

    // SHA-256 of the raw invitation token. The raw token only ever travels in the email.
    public string? InvitationTokenHash { get; private set; }
    public DateTime? InvitationExpiresAt { get; private set; }

    public string? AdminNotes { get; private set; }

    // Waitlist entries only. Stored as the enum name, so the frontend keeps the display labels.
    public ExpectedMonthlyVolume? ExpectedMonthlyVolume { get; private set; }
    public List<TenantUseCase> UseCases { get; private set; } = [];

    public DateTime CreatedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    // Set once the row graduates to a permanent Tenant.
    public Guid? TenantId { get; private set; }

    public bool IsOpen => Status is TemporaryTenantStatus.WaitlistPending or TemporaryTenantStatus.PendingVerification;

    // True while an invitation link or an OTP can still be used. An expired challenge no longer blocks new requests.
    public bool HasLiveChallenge(DateTime nowUtc) =>
        (InvitationTokenHash is not null && InvitationExpiresAt is { } invitationExpiresAt && nowUtc <= invitationExpiresAt)
        || (OtpCode is not null && OtpExpiresAt is { } otpExpiresAt && nowUtc <= otpExpiresAt);

    public static Result<TemporaryTenant> CreateWaitlistEntry(
        string tenantName,
        string email,
        string firstName,
        string lastName,
        string? expectedMonthlyVolume,
        IEnumerable<string>? useCases,
        DateTime createdAtUtc)
    {
        if (!Enum.TryParse<ExpectedMonthlyVolume>(expectedMonthlyVolume, out var volume) || !Enum.IsDefined(volume))
        {
            return TemporaryTenantErrors.InvalidExpectedMonthlyVolume;
        }

        var parsedUseCases = new List<TenantUseCase>();
        foreach (var raw in useCases ?? [])
        {
            if (!Enum.TryParse<TenantUseCase>(raw?.Trim(), out var useCase) || !Enum.IsDefined(useCase))
            {
                return TemporaryTenantErrors.InvalidUseCase;
            }

            if (!parsedUseCases.Contains(useCase))
            {
                parsedUseCases.Add(useCase);
            }
        }

        if (parsedUseCases.Count == 0)
        {
            return TemporaryTenantErrors.UseCasesRequired;
        }

        var created = Create(tenantName, email, firstName, lastName, null, TemporaryTenantSource.Waitlist, TemporaryTenantStatus.WaitlistPending, null, createdAtUtc);
        if (created.IsFailure)
        {
            return created;
        }

        created.Value.ExpectedMonthlyVolume = volume;
        created.Value.UseCases = parsedUseCases;
        return created;
    }

    public static Result<TemporaryTenant> CreateAdminInvitation(string tenantName, string email, string firstName, string lastName, string? adminNotes, DateTime createdAtUtc) =>
        Create(tenantName, email, firstName, lastName, null, TemporaryTenantSource.Admin, TemporaryTenantStatus.PendingVerification, adminNotes, createdAtUtc);

    public static Result<TemporaryTenant> CreateSelfService(string tenantName, string email, string firstName, string lastName, string passwordHash, DateTime createdAtUtc) =>
        Create(tenantName, email, firstName, lastName, passwordHash, TemporaryTenantSource.SelfService, TemporaryTenantStatus.PendingVerification, null, createdAtUtc);

    // Generates the raw token for the caller to email. Only its hash is kept.
    public static string GenerateInvitationToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static string HashInvitationToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    public static string GenerateOtpCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    // Moves an open row into the invitation path. Any previous OTP is dropped.
    public Result StartInvitation(string invitationTokenHash, DateTime nowUtc)
    {
        if (!IsOpen)
        {
            return TemporaryTenantErrors.NotAwaitingVerification;
        }

        Status = TemporaryTenantStatus.PendingVerification;
        InvitationTokenHash = invitationTokenHash;
        InvitationExpiresAt = nowUtc.Add(InvitationLifetime);
        return Result.Success();
    }

    // Moves an open row into the OTP path with a fresh code and a fresh attempt counter.
    public Result StartOtpVerification(string otpCode, DateTime nowUtc)
    {
        if (!IsOpen)
        {
            return TemporaryTenantErrors.NotAwaitingVerification;
        }

        Status = TemporaryTenantStatus.PendingVerification;
        OtpCode = otpCode;
        OtpExpiresAt = nowUtc.Add(OtpLifetime);
        OtpAttemptsCount = 0;
        return Result.Success();
    }

    // Counts every attempt, including wrong ones. The caller must save the row even when this returns a failure,
    // otherwise the attempt is not recorded and the cap does nothing.
    public Result VerifyOtp(string otpCode, DateTime nowUtc)
    {
        if (Status != TemporaryTenantStatus.PendingVerification || OtpCode is null || OtpExpiresAt is not { } expiresAt)
        {
            return TemporaryTenantErrors.NotAwaitingVerification;
        }

        if (OtpAttemptsCount >= OtpMaxAttempts)
        {
            return TemporaryTenantErrors.TooManyVerificationAttempts;
        }

        if (nowUtc > expiresAt)
        {
            return TemporaryTenantErrors.VerificationCodeExpired;
        }

        OtpAttemptsCount++;
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(otpCode), Encoding.UTF8.GetBytes(OtpCode)))
        {
            return TemporaryTenantErrors.InvalidVerificationCode;
        }

        // A code works once.
        OtpCode = null;
        OtpExpiresAt = null;
        return Result.Success();
    }

    public Result<string> GetOtpForResend(DateTime nowUtc)
    {
        if (Status != TemporaryTenantStatus.PendingVerification || OtpCode is null || OtpExpiresAt is not { } expiresAt)
        {
            return TemporaryTenantErrors.NoLiveChallengeToResend;
        }

        return nowUtc > expiresAt
            ? TemporaryTenantErrors.NoLiveChallengeToResend
            : OtpCode;
    }

    public Result EnsureInvitationUsable(DateTime nowUtc)
    {
        if (Status != TemporaryTenantStatus.PendingVerification || InvitationTokenHash is null || InvitationExpiresAt is not { } expiresAt)
        {
            return TemporaryTenantErrors.InvitationNotFound;
        }

        return nowUtc > expiresAt ? TemporaryTenantErrors.InvitationExpired : Result.Success();
    }

    // Called after the permanent Tenant and TenantUser have been staged. Clears every secret the row holds.
    public Result MarkApproved(Guid tenantId, DateTime nowUtc)
    {
        if (Status != TemporaryTenantStatus.PendingVerification)
        {
            return TemporaryTenantErrors.NotAwaitingVerification;
        }

        Status = TemporaryTenantStatus.Approved;
        TenantId = tenantId;
        ApprovedAt = nowUtc;
        PasswordHash = null;
        OtpCode = null;
        OtpExpiresAt = null;
        InvitationTokenHash = null;
        InvitationExpiresAt = null;
        return Result.Success();
    }

    private static Result<TemporaryTenant> Create(
        string tenantName,
        string email,
        string firstName,
        string lastName,
        string? passwordHash,
        TemporaryTenantSource source,
        TemporaryTenantStatus status,
        string? adminNotes,
        DateTime createdAtUtc)
    {
        var normalisedTenantName = tenantName.Trim();
        if (normalisedTenantName.Length is 0 or > MaxTenantNameLength)
        {
            return TenantErrors.InvalidName;
        }

        var normalisedEmail = EmailAddress.Normalise(email);
        if (!EmailAddress.IsValid(normalisedEmail))
        {
            return TenantUserErrors.InvalidEmail;
        }

        var normalisedFirstName = firstName.Trim();
        if (normalisedFirstName.Length is 0 or > MaxNameLength)
        {
            return TenantUserErrors.InvalidFirstName;
        }

        var normalisedLastName = lastName.Trim();
        if (normalisedLastName.Length is 0 or > MaxNameLength)
        {
            return TenantUserErrors.InvalidLastName;
        }

        return new TemporaryTenant(
            Guid.NewGuid(),
            normalisedTenantName,
            normalisedEmail,
            normalisedFirstName,
            normalisedLastName,
            passwordHash,
            source,
            status,
            string.IsNullOrWhiteSpace(adminNotes) ? null : adminNotes.Trim(),
            createdAtUtc);
    }
}
