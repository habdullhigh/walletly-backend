namespace Walletly.Application.DTOs.Onboarding;

public class ResendOtpRequest
{
    /// <summary>
    /// The opaque reference returned by POST /api/tenant/auth/register.
    /// Either this or Email must be supplied.
    /// </summary>
    public string? TwoFactorToken { get; set; }

    /// <summary>
    /// Normalised email address. Used when TwoFactorToken is not available.
    /// </summary>
    public string? Email { get; set; }
}
