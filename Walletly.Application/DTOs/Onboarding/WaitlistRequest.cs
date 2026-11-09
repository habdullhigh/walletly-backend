namespace Walletly.Application.DTOs.Onboarding;

public class WaitlistRequest
{
    public string TenantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Sent as the enum name, e.g. "UnderTenMillion". Validated in TemporaryTenant.CreateWaitlistEntry.
    public string? ExpectedMonthlyVolume { get; set; }

    // Sent as enum names, e.g. ["Wallets", "Payouts"]. At least one is required.
    public List<string> UseCases { get; set; } = [];
}
