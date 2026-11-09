namespace Walletly.Application.DTOs.Onboarding;

public class VerifyOtpRequest
{
    public string? TwoFactorToken { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Required only when the request was approved from the waitlist, because no password was collected yet.
    public string? Password { get; set; }
}
