using Walletly.Domain.Entities;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;

namespace Walletly.Infrastructure.Tests.Onboarding;

public sealed class TemporaryTenantTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Otp_LocksAfterFiveFailedAttempts_EvenWithTheRightCode()
    {
        var tenant = SelfServiceWithOtp("123456");

        for (var attempt = 0; attempt < TemporaryTenant.OtpMaxAttempts; attempt++)
        {
            Assert.Equal(TemporaryTenantErrors.InvalidVerificationCode, tenant.VerifyOtp("000000", Now).Error);
        }

        Assert.Equal(TemporaryTenantErrors.TooManyVerificationAttempts, tenant.VerifyOtp("123456", Now).Error);
        Assert.Equal(TemporaryTenant.OtpMaxAttempts, tenant.OtpAttemptsCount);
    }

    [Fact]
    public void Otp_WorksOnce()
    {
        var tenant = SelfServiceWithOtp("123456");

        Assert.True(tenant.VerifyOtp("123456", Now).IsSuccess);
        Assert.Equal(TemporaryTenantErrors.NotAwaitingVerification, tenant.VerifyOtp("123456", Now).Error);
    }

    [Fact]
    public void Otp_Expired_FailsWithoutCountingAnAttempt()
    {
        var tenant = SelfServiceWithOtp("123456");

        var result = tenant.VerifyOtp("123456", Now + TemporaryTenant.OtpLifetime + TimeSpan.FromSeconds(1));

        Assert.Equal(TemporaryTenantErrors.VerificationCodeExpired, result.Error);
        Assert.Equal(0, tenant.OtpAttemptsCount);
    }

    [Fact]
    public void Otp_Resend_ReturnsTheSameLiveCodeWithoutChangingExpiryOrAttempts()
    {
        var tenant = SelfServiceWithOtp("123456");
        var resendAt = Now + TimeSpan.FromMinutes(2);
        var expiresAt = tenant.OtpExpiresAt;

        var result = tenant.GetOtpForResend(resendAt);

        Assert.True(result.IsSuccess);
        Assert.Equal("123456", result.Value);
        Assert.Equal(expiresAt, tenant.OtpExpiresAt);
        Assert.Equal(0, tenant.OtpAttemptsCount);
    }

    [Fact]
    public void Invitation_IsUsable_UntilSeventyTwoHours()
    {
        var tenant = AdminInvitation(out _);

        Assert.True(tenant.EnsureInvitationUsable(Now + TimeSpan.FromHours(71)).IsSuccess);
        Assert.Equal(TemporaryTenantErrors.InvitationExpired, tenant.EnsureInvitationUsable(Now + TimeSpan.FromHours(73)).Error);
    }

    [Fact]
    public void InvitationToken_IsStoredAsHash_NotAsRawToken()
    {
        var tenant = AdminInvitation(out var rawToken);

        Assert.NotEqual(rawToken, tenant.InvitationTokenHash);
        Assert.Equal(TemporaryTenant.HashInvitationToken(rawToken), tenant.InvitationTokenHash);
        Assert.Matches("^[0-9a-f]{64}$", tenant.InvitationTokenHash!);
    }

    [Fact]
    public void MarkApproved_ClearsEverySecret()
    {
        var tenant = SelfServiceWithOtp("123456");
        var tenantId = Guid.NewGuid();

        var result = tenant.MarkApproved(tenantId, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(TemporaryTenantStatus.Approved, tenant.Status);
        Assert.Equal(tenantId, tenant.TenantId);
        Assert.Null(tenant.PasswordHash);
        Assert.Null(tenant.OtpCode);
        Assert.Null(tenant.InvitationTokenHash);
    }

    [Fact]
    public void LiveChallenge_IsFalse_ForWaitlistEntries()
    {
        var entry = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", "UnderTenMillion", ["Wallets"], Now).Value;

        Assert.False(entry.HasLiveChallenge(Now));
        Assert.True(entry.IsOpen);
    }

    [Fact]
    public void LiveChallenge_ExpiresWithItsCode()
    {
        var tenant = SelfServiceWithOtp("123456");

        Assert.True(tenant.HasLiveChallenge(Now + TemporaryTenant.OtpLifetime));
        Assert.False(tenant.HasLiveChallenge(Now + TemporaryTenant.OtpLifetime + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Create_NormalisesEmail_AndRejectsMalformedOnes()
    {
        var valid = TemporaryTenant.CreateWaitlistEntry("Acme", "  Ada@Example.COM ", "Ada", "Obi", "UnderTenMillion", ["Wallets"], Now);
        var invalid = TemporaryTenant.CreateWaitlistEntry("Acme", "not-an-email", "Ada", "Obi", "UnderTenMillion", ["Wallets"], Now);

        Assert.Equal("ada@example.com", valid.Value.Email);
        Assert.Equal(TenantUserErrors.InvalidEmail, invalid.Error);
    }

    [Fact]
    public void WaitlistEntry_StoresVolumeAndDeduplicatedUseCases()
    {
        var entry = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", "TenToHundredMillion", ["Wallets", "Payouts", "Wallets"], Now).Value;

        Assert.Equal(ExpectedMonthlyVolume.TenToHundredMillion, entry.ExpectedMonthlyVolume);
        Assert.Equal([TenantUseCase.Wallets, TenantUseCase.Payouts], entry.UseCases);
    }

    [Fact]
    public void WaitlistEntry_RejectsUnknownVolume()
    {
        var result = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", "Under10m", ["Wallets"], Now);

        Assert.Equal(TemporaryTenantErrors.InvalidExpectedMonthlyVolume, result.Error);
    }

    [Fact]
    public void WaitlistEntry_RejectsMissingVolume()
    {
        var result = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", null, ["Wallets"], Now);

        Assert.Equal(TemporaryTenantErrors.InvalidExpectedMonthlyVolume, result.Error);
    }

    [Fact]
    public void WaitlistEntry_RejectsUnknownUseCase()
    {
        var result = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", "PreLaunch", ["Wallets", "Crypto"], Now);

        Assert.Equal(TemporaryTenantErrors.InvalidUseCase, result.Error);
    }

    [Fact]
    public void WaitlistEntry_RequiresAtLeastOneUseCase()
    {
        var empty = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", "PreLaunch", [], Now);
        var missing = TemporaryTenant.CreateWaitlistEntry("Acme", "ada@example.com", "Ada", "Obi", "PreLaunch", null, Now);

        Assert.Equal(TemporaryTenantErrors.UseCasesRequired, empty.Error);
        Assert.Equal(TemporaryTenantErrors.UseCasesRequired, missing.Error);
    }

    [Fact]
    public void Activate_MovesPendingTenantToActive_AndOnlyOnce()
    {
        var tenant = Tenant.CreatePendingApproval("Acme", Now).Value;

        Assert.False(tenant.IsLiveModeEnabled);
        Assert.True(tenant.Activate().IsSuccess);
        Assert.True(tenant.IsLiveModeEnabled);
        Assert.Equal(TenantErrors.AlreadyActive, tenant.Activate().Error);
    }

    private static TemporaryTenant SelfServiceWithOtp(string code)
    {
        var tenant = TemporaryTenant.CreateSelfService("Acme", "ada@example.com", "Ada", "Obi", "hash", Now).Value;
        tenant.StartOtpVerification(code, Now);
        return tenant;
    }

    private static TemporaryTenant AdminInvitation(out string rawToken)
    {
        var tenant = TemporaryTenant.CreateAdminInvitation("Beta Co", "owner@beta.test", "Ben", "Adu", null, Now).Value;
        rawToken = TemporaryTenant.GenerateInvitationToken();
        tenant.StartInvitation(TemporaryTenant.HashInvitationToken(rawToken), Now);
        return tenant;
    }
}
