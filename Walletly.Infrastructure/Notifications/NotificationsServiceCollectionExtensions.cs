using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Configuration;
using Walletly.Infrastructure.Notifications.Jobs;
using Walletly.Infrastructure.Notifications.Providers;
using Walletly.Infrastructure.Notifications.Templates;

namespace Walletly.Infrastructure.Notifications;

public static class NotificationsServiceCollectionExtensions
{
    // Startup fails with a clear message if Email:Provider is unknown or the active provider's settings are incomplete.
    public static IServiceCollection AddEmailNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<BrevoOptions>()
            .Bind(configuration.GetSection(BrevoOptions.SectionName));

        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();

        services.TryAddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailProviderSelector, EmailProviderSelector>();
        services.AddTransient<EmailSendJob>();

        services.AddTransient<RetryAfterDelegatingHandler>();
        services.AddHttpClient<BrevoEmailProvider>((serviceProvider, client) =>
            {
                var brevo = serviceProvider.GetRequiredService<IOptions<BrevoOptions>>().Value;
                client.BaseAddress = new Uri(brevo.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(brevo.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("api-key", brevo.ApiKey);
            })
            .AddHttpMessageHandler<RetryAfterDelegatingHandler>();

        // Keyed by provider name. Adding a provider means one new keyed registration and one options class.
        services.AddKeyedScoped<IEmailProvider>(
            BrevoEmailProvider.Key,
            (serviceProvider, _) => serviceProvider.GetRequiredService<BrevoEmailProvider>());

        return services;
    }
}
