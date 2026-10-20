namespace Walletly.Application.DTOs.Tenants;

public class CreatedApiKeyDto
{
    public ApiKeyDto ApiKey { get; set; } = null!;

    // Shown once. It cannot be retrieved again.
    public string Key { get; set; } = string.Empty;
}
