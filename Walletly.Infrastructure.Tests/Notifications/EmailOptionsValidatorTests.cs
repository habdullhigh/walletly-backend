using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Walletly.Infrastructure.Notifications;
using Walletly.Infrastructure.Notifications.Configuration;

namespace Walletly.Infrastructure.Tests.Notifications;

public sealed class EmailOptionsValidatorTests
{
    [Fact]
    public void UnknownProvider_Fails_NamingTheProvider()
    {
        var validator = new EmailOptionsValidator(Options.Create(ValidBrevo()));

        var result = validator.Validate(null, ValidEmail("Smtp"));

        Assert.True(result.Failed);
        Assert.Contains("'Smtp' is not supported", result.FailureMessage);
    }

    [Fact]
    public void MissingBrevoApiKey_Fails_WithoutEchoingAnyKey()
    {
        var validator = new EmailOptionsValidator(Options.Create(new BrevoOptions { ApiKey = string.Empty }));

        var result = validator.Validate(null, ValidEmail("Brevo"));

        Assert.True(result.Failed);
        Assert.Contains("ApiKey", result.FailureMessage);
    }

    [Fact]
    public void ProviderName_IsMatchedCaseInsensitively()
    {
        var validator = new EmailOptionsValidator(Options.Create(ValidBrevo()));

        var result = validator.Validate(null, ValidEmail("BREVO"));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UnknownProvider_StopsTheHost_AtStartup()
    {
        using var host = BuildHost(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Smtp",
            ["Email:SenderEmail"] = "noreply@walletly.test",
            ["Email:SenderName"] = "Walletly",
            ["Email:Brevo:ApiKey"] = "test-key"
        });

        var exception = await Assert.ThrowsAnyAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains("'Smtp' is not supported", exception.Message);
    }

    [Fact]
    public async Task CompleteConfiguration_StartsTheHost()
    {
        using var host = BuildHost(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Brevo",
            ["Email:SenderEmail"] = "noreply@walletly.test",
            ["Email:SenderName"] = "Walletly",
            ["Email:Brevo:ApiKey"] = "test-key"
        });

        await host.StartAsync();
        await host.StopAsync();
    }

    private static IHost BuildHost(Dictionary<string, string?> settings)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Services.AddEmailNotifications(builder.Configuration);
        return builder.Build();
    }

    private static EmailOptions ValidEmail(string provider) => new()
    {
        Provider = provider,
        SenderEmail = "noreply@walletly.test",
        SenderName = "Walletly"
    };

    private static BrevoOptions ValidBrevo() => new() { ApiKey = "test-key" };
}
