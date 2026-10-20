using SharedKernel;
using Walletly.Application.Abstractions.Notifications;

namespace Walletly.Infrastructure.Notifications.Providers;

public interface IEmailProvider
{
    // Expected failures come back as Result.Failure. Providers do not throw for them.
    Task<Result<EmailSendResult>> SendAsync(RenderedEmail email, CancellationToken cancellationToken);
}

public sealed record RenderedEmail(
    EmailAddressee Sender,
    IReadOnlyList<EmailAddressee> To,
    string Subject,
    string HtmlContent,
    IReadOnlyList<EmailAddressee>? Cc = null,
    IReadOnlyList<EmailAddressee>? Bcc = null,
    EmailAddressee? ReplyTo = null,
    IReadOnlyList<EmailAttachment>? Attachments = null,
    IReadOnlyList<string>? Tags = null);

public sealed record EmailSendResult(string ProviderMessageId);
