using Walletly.Application.Abstractions.Notifications;

namespace Walletly.Infrastructure.Notifications.Jobs;

// Hangfire stores job arguments as JSON via Newtonsoft, which cannot rebuild the IReadOnly* collection types
// that EmailMessage uses. The job takes this concrete-typed copy instead. New fields go here and in From().
public sealed record EmailSendPayload(
    string TemplateKey,
    List<EmailAddressee> To,
    Dictionary<string, string> Tokens,
    List<EmailAddressee>? Cc,
    List<EmailAddressee>? Bcc,
    EmailAddressee? ReplyTo,
    List<EmailAttachment>? Attachments,
    List<string>? Tags)
{
    public static EmailSendPayload From(EmailMessage message) => new(
        message.TemplateKey,
        message.To.ToList(),
        new Dictionary<string, string>(message.Tokens),
        message.Cc?.ToList(),
        message.Bcc?.ToList(),
        message.ReplyTo,
        message.Attachments?.ToList(),
        message.Tags?.ToList());
}
