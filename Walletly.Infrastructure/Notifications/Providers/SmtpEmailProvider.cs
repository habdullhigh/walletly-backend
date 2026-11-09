using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Configuration;

namespace Walletly.Infrastructure.Notifications.Providers;

public sealed class SmtpEmailProvider(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<SmtpEmailProvider> logger) : IEmailProvider
{
    // Keyed-service name. Matches Email:Provider "Smtp" once lowercased.
    public const string Key = "smtp";

    public async Task<Result<EmailSendResult>> SendAsync(RenderedEmail email, CancellationToken cancellationToken)
    {
        var options = smtpOptions.Value;

        var message = BuildMessage(email);

        using var client = new SmtpClient();
        try
        {
            var socketOptions = options.UseSsl
                ? SecureSocketOptions.SslOnConnect          // port 465 — SSL/TLS from the start
                : SecureSocketOptions.StartTlsWhenAvailable; // port 587 — plain then STARTTLS

            await client.ConnectAsync(options.Host, options.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                await client.AuthenticateAsync(options.UserName, options.Password, cancellationToken);
            }

            var messageId = await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            return new EmailSendResult(messageId ?? string.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Log without echoing credentials or message content.
            logger.LogWarning(ex, "SMTP send failed for host {Host}:{Port}.", options.Host, options.Port);
            return IsTransient(ex)
                ? NotificationErrors.ProviderUnavailable
                : NotificationErrors.ProviderRejected;
        }
    }

    private static MimeMessage BuildMessage(RenderedEmail email)
    {
        var message = new MimeMessage();

        message.From.Add(ToMailboxAddress(email.Sender));

        foreach (var to in email.To)
        {
            message.To.Add(ToMailboxAddress(to));
        }

        if (email.Cc is not null)
        {
            foreach (var cc in email.Cc)
            {
                message.Cc.Add(ToMailboxAddress(cc));
            }
        }

        if (email.Bcc is not null)
        {
            foreach (var bcc in email.Bcc)
            {
                message.Bcc.Add(ToMailboxAddress(bcc));
            }
        }

        if (email.ReplyTo is not null)
        {
            message.ReplyTo.Add(ToMailboxAddress(email.ReplyTo));
        }

        message.Subject = email.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = email.HtmlContent };

        if (email.Attachments is not null)
        {
            foreach (var attachment in email.Attachments)
            {
                var bytes = Convert.FromBase64String(attachment.ContentBase64);
                bodyBuilder.Attachments.Add(attachment.FileName, bytes);
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static MailboxAddress ToMailboxAddress(EmailAddressee addressee) =>
        new(addressee.Name ?? string.Empty, addressee.Email);

    // Network errors, timeouts, and SMTP 4xx codes (which are transient by SMTP spec) are retriable.
    // MailKit throws AuthenticationException (permanent) and SmtpCommandException with separate status codes.
    private static bool IsTransient(Exception ex) =>
        ex is MailKit.Net.Smtp.SmtpCommandException smtp
            ? (int)smtp.StatusCode >= 400 && (int)smtp.StatusCode < 500
            : ex is MailKit.Net.Smtp.SmtpProtocolException
              || ex is System.IO.IOException
              || ex is System.Net.Sockets.SocketException;
}
