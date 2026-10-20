namespace Walletly.Infrastructure.Security;

public class WebhookSecretSettings
{
    public const string SectionName = "WebhookSecrets";

    // Base64-encoded 256-bit AES key. Set it in user secrets or the environment, never in appsettings.
    public string EncryptionKey { get; set; } = string.Empty;
}
