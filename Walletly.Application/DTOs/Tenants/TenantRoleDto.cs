namespace Walletly.Application.DTOs.Tenants;

public class TenantRoleDto
{
    public Guid Id { get; set; }
    public Guid TenantEnvironmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
