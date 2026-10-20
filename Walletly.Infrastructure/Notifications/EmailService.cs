using System.ComponentModel.DataAnnotations;
using Hangfire;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Jobs;
using Walletly.Infrastructure.Notifications.Templates;

namespace Walletly.Infrastructure.Notifications;

internal sealed class EmailService(IEmailTemplateRenderer renderer, IBackgroundJobClient backgroundJobs) : IEmailService
{
    private static readonly EmailAddressAttribute EmailAddressValidator = new();

    public Task<Result> EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!AreValid(message))
        {
            return Task.FromResult(Result.Failure(NotificationErrors.InvalidRecipient));
        }

        // Check the template and tokens now so the caller gets the failure. Nothing is queued.
        // The job renders again when it runs.
        var rendered = renderer.Render(message.TemplateKey, message.Tokens);
        if (rendered.IsFailure)
        {
            return Task.FromResult(Result.Failure(rendered.Error));
        }

        var payload = EmailSendPayload.From(message);
        backgroundJobs.Enqueue<EmailSendJob>(job => job.ExecuteAsync(payload, 1));

        return Task.FromResult(Result.Success());
    }

    private static bool AreValid(EmailMessage message)
    {
        if (message.To.Count == 0)
        {
            return false;
        }

        var addresses = message.To
            .Concat(message.Cc ?? [])
            .Concat(message.Bcc ?? [])
            .Concat(message.ReplyTo is null ? [] : [message.ReplyTo]);

        return addresses.All(address => EmailAddressValidator.IsValid(address.Email));
    }
}
