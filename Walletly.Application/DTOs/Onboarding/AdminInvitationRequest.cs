namespace Walletly.Application.DTOs.Onboarding;

public class AdminInvitationRequest
{
    public string TenantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
}
