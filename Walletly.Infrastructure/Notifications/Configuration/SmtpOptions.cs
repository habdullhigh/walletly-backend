using System.ComponentModel.DataAnnotations;

namespace Walletly.Infrastructure.Notifications.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    // UseSsl = true  → connect with SSL/TLS on port 465.
    // UseSsl = false → connect in plain-text and upgrade with STARTTLS if the server offers it (port 587 default).
    public bool UseSsl { get; set; } = false;

    // Leave blank to send without authentication (e.g. a local relay that trusts the host).
    public string UserName { get; set; } = string.Empty;

    // Secret. Set via Email__Smtp__Password environment variable or user secrets. Never commit a real password.
    public string Password { get; set; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}
