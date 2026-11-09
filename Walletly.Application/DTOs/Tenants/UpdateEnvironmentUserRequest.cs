namespace Walletly.Application.DTOs.Tenants;

public class UpdateEnvironmentUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
}
