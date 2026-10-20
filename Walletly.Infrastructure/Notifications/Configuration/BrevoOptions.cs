using System.ComponentModel.DataAnnotations;

namespace Walletly.Infrastructure.Notifications.Configuration;

public sealed class BrevoOptions
{
    public const string SectionName = "Email:Brevo";

    // Secret. Set it with the Email__Brevo__ApiKey environment variable or user secrets. Never commit a real key.
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Required, Url]
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}
