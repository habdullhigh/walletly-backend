namespace Walletly.Application.DTOs.Onboarding;

public class TemporaryTenantDto
{
    public Guid Id { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExpectedMonthlyVolume { get; set; }
    public List<string> UseCases { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
