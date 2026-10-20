using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Walletly.Infrastructure.Notifications.Configuration;

namespace Walletly.Infrastructure.Notifications.Providers;

public interface IEmailProviderSelector
{
    IEmailProvider Resolve();
}

// The one place that maps Email:Provider to a keyed IEmailProvider.
// EmailOptionsValidator guarantees the name is known at startup, so the lookup cannot fail at send time.
internal sealed class EmailProviderSelector(IServiceProvider services, IOptions<EmailOptions> options) : IEmailProviderSelector
{
    public IEmailProvider Resolve() =>
        services.GetRequiredKeyedService<IEmailProvider>(options.Value.Provider.ToLowerInvariant());
}
