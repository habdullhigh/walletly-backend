namespace Walletly.Application.DTOs.Tenants;

public class CreateEnvironmentUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string? ExternalReference { get; set; }
}
