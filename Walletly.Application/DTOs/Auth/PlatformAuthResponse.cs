namespace Walletly.Application.DTOs.Auth;

public class PlatformAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public PlatformUserDto User { get; set; } = null!;
}
