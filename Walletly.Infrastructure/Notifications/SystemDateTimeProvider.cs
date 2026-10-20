using SharedKernel;

namespace Walletly.Infrastructure.Notifications;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
