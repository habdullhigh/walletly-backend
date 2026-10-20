using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications;

namespace Walletly.Infrastructure.Tests.Notifications;

public sealed class EmailServiceTests
{
    [Fact]
    public async Task ValidMessage_IsEnqueued()
    {
        var (service, backgroundJobs) = CreateService();

        var result = await service.EnqueueAsync(OtpMessage());

        Assert.True(result.IsSuccess);
        Assert.Single(backgroundJobs.Created);
    }

    [Fact]
    public async Task UnknownTemplate_IsRejected_AndNothingIsQueued()
    {
        var (service, backgroundJobs) = CreateService();

        var result = await service.EnqueueAsync(OtpMessage() with { TemplateKey = "no-such-template" });

        Assert.Equal(NotificationErrors.TemplateNotFound, result.Error);
        Assert.Empty(backgroundJobs.Created);
    }

    [Fact]
    public async Task MissingToken_IsRejected_AndNothingIsQueued()
    {
        var (service, backgroundJobs) = CreateService();
        var tokens = new Dictionary<string, string> { ["otp_code"] = "482913" };

        var result = await service.EnqueueAsync(OtpMessage() with { Tokens = tokens });

        Assert.Equal(NotificationErrors.MissingToken, result.Error);
        Assert.Empty(backgroundJobs.Created);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public async Task InvalidRecipient_IsRejected(string email)
    {
        var (service, backgroundJobs) = CreateService();

        var result = await service.EnqueueAsync(OtpMessage() with { To = [new EmailAddressee(email)] });

        Assert.Equal(NotificationErrors.InvalidRecipient, result.Error);
        Assert.Empty(backgroundJobs.Created);
    }

    [Fact]
    public async Task NoRecipients_IsRejected()
    {
        var (service, backgroundJobs) = CreateService();

        var result = await service.EnqueueAsync(OtpMessage() with { To = [] });

        Assert.Equal(NotificationErrors.InvalidRecipient, result.Error);
        Assert.Empty(backgroundJobs.Created);
    }

    private static (IEmailService Service, FakeBackgroundJobClient BackgroundJobs) CreateService()
    {
        var backgroundJobs = new FakeBackgroundJobClient();
        var services = new ServiceCollection();
        services.AddSingleton<Hangfire.IBackgroundJobClient>(backgroundJobs);
        services.AddEmailNotifications(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:Provider"] = "Brevo",
                ["Email:SenderEmail"] = "noreply@walletly.test",
                ["Email:SenderName"] = "Walletly",
                ["Email:Brevo:ApiKey"] = "test-key"
            })
            .Build());

        var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IEmailService>(), backgroundJobs);
    }

    private static EmailMessage OtpMessage() => new(
        TemplateKey: "otp-verification",
        To: [new EmailAddressee("ada@example.com", "Ada")],
        Tokens: new Dictionary<string, string>
        {
            ["otp_code"] = "482913",
            ["first_name"] = "Ada",
            ["expires_in_minutes"] = "10"
        });
}
