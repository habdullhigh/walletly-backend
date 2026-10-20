using SharedKernel;

namespace Walletly.Application.Abstractions.Notifications;

public interface IEmailService
{
    // Returns the outcome of enqueueing only. Provider delivery happens later in a background job.
    Task<Result> EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
