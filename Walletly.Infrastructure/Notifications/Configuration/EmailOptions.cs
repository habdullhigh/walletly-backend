using System.ComponentModel.DataAnnotations;

namespace Walletly.Infrastructure.Notifications.Configuration;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    // Only "Brevo" is supported today. Adding a provider means adding it to EmailOptionsValidator too.
    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The sender address must be verified in Brevo (Senders &amp; IP) before any mail goes out.
    /// Brevo rejects messages from unverified senders.
    /// </summary>
    [Required, EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;

    [Required]
    public string SenderName { get; set; } = string.Empty;
}
