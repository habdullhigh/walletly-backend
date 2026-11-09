namespace Walletly.Application.DTOs.Onboarding;

public class TenantRegistrationResponse
{
    public bool RequiresTwoFactor { get; set; } = true;
    public string Message { get; set; } = "A one-time passcode has been sent to your registered email address.";
    public string TwoFactorToken { get; set; } = string.Empty;
    public bool IsDefaultPasswordUser { get; set; }
    public string? DefaultPasswordToken { get; set; }
}