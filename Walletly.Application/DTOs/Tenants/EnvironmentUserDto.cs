namespace Walletly.Application.DTOs.Tenants;

public class EnvironmentUserDto
{
    public Guid Id { get; set; }
    public Guid TenantEnvironmentId { get; set; }
    public Guid? TenantRoleId { get; set; }
    public string? RoleCode { get; set; }
    public string? RoleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Permissions { get; set; } = [];
}
