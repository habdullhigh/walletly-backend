namespace Walletly.Application.DTOs.Tenants;

public class LoginTenantUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
