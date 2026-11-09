namespace Walletly.Application.DTOs.Tenants;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // PendingApproval tenants have sandbox access only.
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TenantEnvironmentDto> Environments { get; set; } = new();
}
