namespace Walletly.Application.DTOs.Tenants;

public class TenantAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public TenantUserDto User { get; set; } = null!;
}
