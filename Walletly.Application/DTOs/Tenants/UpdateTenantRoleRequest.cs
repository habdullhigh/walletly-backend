namespace Walletly.Application.DTOs.Tenants;

public class UpdateTenantRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
}
