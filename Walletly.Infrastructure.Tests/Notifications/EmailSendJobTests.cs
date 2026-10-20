using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Configuration;
using Walletly.Infrastructure.Notifications.Jobs;
using Walletly.Infrastructure.Notifications.Providers;
using Walletly.Infrastructure.Notifications.Templates;

namespace Walletly.Infrastructure.Tests.Notifications;

public sealed class EmailSendJobTests
{
    private const string OtpCode = "482913";

    [Fact]
    public async Task Success_IsLogged_AndNotRetried()
    {
        var provider = new FakeEmailProvider(_ => Result.Success(new EmailSendResult("<ok@brevo>")));
        var (job, backgroundJobs, logs) = CreateJob(provider);

        await job.ExecuteAsync(OtpPayload(), 1);

        Assert.Single(provider.Sent);
        Assert.Empty(backgroundJobs.Created);
        Assert.Contains(logs.Messages, message => message.Contains("accepted on attempt 1"));
    }

    [Fact]
    public async Task RenderedEmail_CarriesSenderFromOptions_AndRenderedSubject()
    {
        var provider = new FakeEmailProvider(_ => Result.Success(new EmailSendResult("<ok@brevo>")));
        var (job, _, _) = CreateJob(provider);

        await job.ExecuteAsync(OtpPayload(), 1);

        var sent = Assert.Single(provider.Sent);
        Assert.Equal("noreply@walletly.test", sent.Sender.Email);
        Assert.Equal("Walletly", sent.Sender.Name);
        Assert.Equal($"Your Walletly verification code is {OtpCode}", sent.Subject);
    }

    [Fact]
    public async Task TransientFailure_SchedulesRetry_WithBackoff()
    {
        var provider = new FakeEmailProvider(_ => NotificationErrors.ProviderUnavailable);
        var (job, backgroundJobs, _) = CreateJob(provider);

        await job.ExecuteAsync(OtpPayload(), 1);

        var (scheduled, state) = Assert.Single(backgroundJobs.Created);
        Assert.IsType<ScheduledState>(state);
        Assert.Equal(2, scheduled.Args[1]);
    }

    [Fact]
    public async Task PermanentFailure_IsNotRetried_AndFailsTheJob()
    {
        var provider = new FakeEmailProvider(_ => NotificationErrors.ProviderRejected);
        var (job, backgroundJobs, _) = CreateJob(provider);

        var exception = await Assert.ThrowsAsync<EmailDeliveryException>(() => job.ExecuteAsync(OtpPayload(), 1));

        Assert.Equal(NotificationErrors.ProviderRejected, exception.Error);
        Assert.Empty(backgroundJobs.Created);
    }

    [Fact]
    public async Task TransientFailure_OnLastAttempt_FailsTheJob()
    {
        var provider = new FakeEmailProvider(_ => NotificationErrors.ProviderUnavailable);
        var (job, backgroundJobs, _) = CreateJob(provider);

        await Assert.ThrowsAsync<EmailDeliveryException>(() => job.ExecuteAsync(OtpPayload(), EmailSendJob.MaxAttempts));

        Assert.Empty(backgroundJobs.Created);
    }

    [Fact]
    public async Task OtpValues_NeverAppear_InLogs_OrInExceptions()
    {
        var logsByScenario = new List<(CapturingLoggerProvider Logs, Func<Task> Run)>();

        var transient = CreateJob(new FakeEmailProvider(_ => NotificationErrors.ProviderUnavailable));
        logsByScenario.Add((transient.Logs, () => transient.Job.ExecuteAsync(OtpPayload(), 1)));

        var permanent = CreateJob(new FakeEmailProvider(_ => NotificationErrors.ProviderRejected));
        logsByScenario.Add((permanent.Logs, () => permanent.Job.ExecuteAsync(OtpPayload(), 1)));

        var success = CreateJob(new FakeEmailProvider(_ => Result.Success(new EmailSendResult("<ok@brevo>"))));
        logsByScenario.Add((success.Logs, () => success.Job.ExecuteAsync(OtpPayload(), 1)));

        foreach (var (logs, run) in logsByScenario)
        {
            try
            {
                await run();
            }
            catch (EmailDeliveryException exception)
            {
                Assert.DoesNotContain(OtpCode, exception.Message);
            }

            Assert.All(logs.Messages, message => Assert.DoesNotContain(OtpCode, message));
        }
    }

    [Fact]
    public void Payload_SurvivesHangfireSerialization()
    {
        var payload = EmailSendPayload.From(OtpMessage() with
        {
            Attachments = [new EmailAttachment("notice.txt", "aGVsbG8=")],
            Tags = ["otp"]
        });

        var json = SerializationHelper.Serialize(payload);
        var restored = SerializationHelper.Deserialize<EmailSendPayload>(json);

        Assert.Equal(payload.TemplateKey, restored.TemplateKey);
        Assert.Equal(OtpCode, restored.Tokens["otp_code"]);
        Assert.Equal("ada@example.com", restored.To.Single().Email);
        Assert.Equal("aGVsbG8=", restored.Attachments!.Single().ContentBase64);
    }

    private static EmailSendPayload OtpPayload() => EmailSendPayload.From(OtpMessage());

    private static EmailMessage OtpMessage() => new(
        TemplateKey: "otp-verification",
        To: [new EmailAddressee("ada@example.com", "Ada")],
        Tokens: new Dictionary<string, string>
        {
            ["otp_code"] = OtpCode,
            ["first_name"] = "Ada",
            ["expires_in_minutes"] = "10"
        });

    private static (EmailSendJob Job, FakeBackgroundJobClient BackgroundJobs, CapturingLoggerProvider Logs) CreateJob(FakeEmailProvider provider)
    {
        var backgroundJobs = new FakeBackgroundJobClient();
        var logs = new CapturingLoggerProvider();
        using var loggerFactory = new LoggerFactory([logs]);

        var job = new EmailSendJob(
            new EmailTemplateRenderer(),
            new FakeProviderSelector(provider),
            Options.Create(new EmailOptions
            {
                Provider = "Brevo",
                SenderEmail = "noreply@walletly.test",
                SenderName = "Walletly"
            }),
            backgroundJobs,
            loggerFactory.CreateLogger<EmailSendJob>());

        return (job, backgroundJobs, logs);
    }
}
