namespace Walletly.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Walletly";
    public string Audience { get; set; } = "WalletlyPlatform";
    public int ExpiryMinutes { get; set; } = 1440; // 24 hours
}
