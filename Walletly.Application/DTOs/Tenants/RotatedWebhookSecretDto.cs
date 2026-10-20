namespace Walletly.Application.DTOs.Tenants;

public class RotatedWebhookSecretDto
{
    public string Mode { get; set; } = string.Empty;

    // Shown once. Rotating invalidates the previous secret immediately.
    public string WebhookSecret { get; set; } = string.Empty;
}
