namespace Walletly.Application.DTOs.Tenants;

public class TenantEnvironmentDto
{
    public string Mode { get; set; } = string.Empty;
    public List<string> WebhookUrls { get; set; } = new();
    public List<string> ReturnUrls { get; set; } = new();
}
