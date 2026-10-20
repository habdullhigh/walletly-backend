using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Configuration;
using Walletly.Infrastructure.Notifications.Providers;
using Walletly.Infrastructure.Notifications.Templates;

namespace Walletly.Infrastructure.Notifications.Jobs;

// Hangfire's default retry would also retry permanent failures, so retries are scheduled here instead.
[AutomaticRetry(Attempts = 0)]
public sealed class EmailSendJob(
    IEmailTemplateRenderer renderer,
    IEmailProviderSelector providerSelector,
    IOptions<EmailOptions> options,
    IBackgroundJobClient backgroundJobs,
    ILogger<EmailSendJob> logger)
{
    public const int MaxAttempts = 5;

    // Delay before attempt N+1 is RetryDelays[N - 1]. Retry-After on 429 is handled inside the HTTP pipeline.
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1)];

    // Message bodies and tokens (including OTP codes) are never logged. Only the template key, attempt and error code are.
    public async Task ExecuteAsync(EmailSendPayload payload, int attempt)
    {
        var rendered = renderer.Render(payload.TemplateKey, payload.Tokens);
        if (rendered.IsFailure)
        {
            logger.LogError("Email {TemplateKey} not sent. Error {ErrorCode}.", payload.TemplateKey, rendered.Error.Code);
            throw new EmailDeliveryException(rendered.Error);
        }

        var provider = providerSelector.Resolve();
        var email = new RenderedEmail(
            Sender: new EmailAddressee(options.Value.SenderEmail, options.Value.SenderName),
            To: payload.To,
            Subject: rendered.Value.Subject,
            HtmlContent: rendered.Value.HtmlContent,
            Cc: payload.Cc,
            Bcc: payload.Bcc,
            ReplyTo: payload.ReplyTo,
            Attachments: payload.Attachments,
            Tags: payload.Tags);

        var result = await provider.SendAsync(email, CancellationToken.None);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Email {TemplateKey} accepted on attempt {Attempt}. Provider message id {ProviderMessageId}.",
                payload.TemplateKey, attempt, result.Value.ProviderMessageId);
            return;
        }

        if (IsTransient(result.Error) && attempt < MaxAttempts)
        {
            var delay = RetryDelays[attempt - 1];
            backgroundJobs.Schedule<EmailSendJob>(job => job.ExecuteAsync(payload, attempt + 1), delay);
            logger.LogWarning(
                "Email {TemplateKey} failed on attempt {Attempt} with {ErrorCode}. Retrying in {DelaySeconds} seconds.",
                payload.TemplateKey, attempt, result.Error.Code, delay.TotalSeconds);
            return;
        }

        logger.LogError(
            "Email {TemplateKey} failed on attempt {Attempt} with {ErrorCode}. Not retrying.",
            payload.TemplateKey, attempt, result.Error.Code);
        throw new EmailDeliveryException(result.Error);
    }

    private static bool IsTransient(Error error) => error.Code == NotificationErrors.ProviderUnavailable.Code;
}

// Marks the Hangfire job as Failed in the dashboard. The message carries the error code only.
public sealed class EmailDeliveryException(Error error) : Exception($"{error.Code}: {error.Description}")
{
    public Error Error { get; } = error;
}
