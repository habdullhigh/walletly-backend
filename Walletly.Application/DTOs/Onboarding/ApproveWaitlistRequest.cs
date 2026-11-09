namespace Walletly.Application.DTOs.Onboarding;

public class ApproveWaitlistRequest
{
    // "invitation" (default) sends a 72-hour password link. "otp" sends a 6-digit code instead.
    public string? Method { get; set; }
}
