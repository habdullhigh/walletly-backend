namespace Walletly.Application.Abstractions.Notifications;

public sealed record EmailMessage(
    string TemplateKey,
    IReadOnlyList<EmailAddressee> To,
    IReadOnlyDictionary<string, string> Tokens,
    IReadOnlyList<EmailAddressee>? Cc = null,
    IReadOnlyList<EmailAddressee>? Bcc = null,
    EmailAddressee? ReplyTo = null,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    IReadOnlyList<string>? Tags = null);

public sealed record EmailAddressee(string Email, string? Name = null);

public sealed record EmailAttachment(string FileName, string ContentBase64);
