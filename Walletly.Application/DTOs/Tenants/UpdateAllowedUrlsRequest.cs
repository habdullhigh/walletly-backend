namespace Walletly.Application.DTOs.Tenants;

public class UpdateAllowedUrlsRequest
{
    public List<string> WebhookUrls { get; set; } = new();
    public List<string> ReturnUrls { get; set; } = new();
}
