using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.BackgroundJobs;
using Walletly.Infrastructure.Notifications;
using Walletly.Infrastructure.Notifications.Jobs;

namespace Walletly.Infrastructure.Tests.BackgroundJobs;

// Needs Docker. Runs a real PostgreSQL container and checks that an enqueued email is stored and restored intact.
public sealed class HangfireStorageIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task EnqueuedEmail_IsStoredInPostgres_AndRestoredWithItsTokens()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBackgroundJobs(_postgres.GetConnectionString());
        services.AddEmailNotifications(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Provider"] = "Brevo",
                ["Email:SenderEmail"] = "noreply@walletly.test",
                ["Email:SenderName"] = "Walletly",
                ["Email:Brevo:ApiKey"] = "test-key"
            })
            .Build());

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var message = new EmailMessage(
            TemplateKey: "otp-verification",
            To: [new EmailAddressee("ada@example.com", "Ada")],
            Tokens: new Dictionary<string, string>
            {
                ["otp_code"] = "482913",
                ["first_name"] = "Ada",
                ["expires_in_minutes"] = "10"
            });

        var result = await emailService.EnqueueAsync(message);

        Assert.True(result.IsSuccess);

        var enqueued = JobStorage.Current.GetMonitoringApi().EnqueuedJobs("default", 0, 10);
        var stored = Assert.Single(enqueued).Value;
        Assert.Null(stored.LoadException);
        Assert.NotNull(stored.Job);

        Assert.Equal(typeof(EmailSendJob), stored.Job.Type);
        var restored = Assert.IsType<EmailSendPayload>(stored.Job.Args[0]);
        Assert.Equal("otp-verification", restored.TemplateKey);
        Assert.Equal("482913", restored.Tokens["otp_code"]);
        Assert.Equal("ada@example.com", restored.To.Single().Email);
    }
}
