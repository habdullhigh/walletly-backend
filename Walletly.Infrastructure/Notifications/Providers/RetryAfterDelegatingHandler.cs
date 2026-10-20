using System.Net;
using System.Net.Http.Headers;
using SharedKernel;

namespace Walletly.Infrastructure.Notifications.Providers;

// Honors Retry-After on HTTP 429 with one resend, capped at MaxDelay.
// A 429 means the message was not accepted, so resending cannot duplicate it.
// Other retries are left to the Hangfire job, which applies backoff.
public sealed class RetryAfterDelegatingHandler(IDateTimeProvider clock) : DelegatingHandler
{
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(60);
    private const int MaxAttempts = 2;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);

        for (var attempt = 1; response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxAttempts; attempt++)
        {
            var delay = RetryDelay(response.Headers.RetryAfter);
            if (delay is null)
            {
                break;
            }

            response.Dispose();
            await Task.Delay(delay.Value, cancellationToken);
            response = await base.SendAsync(Clone(request, body), cancellationToken);
        }

        return response;
    }

    private TimeSpan? RetryDelay(RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter is null)
        {
            return null;
        }

        var delay = retryAfter.Delta
            ?? (retryAfter.Date is { } date ? date - clock.UtcNow : (TimeSpan?)null);

        if (delay is null)
        {
            return null;
        }

        return delay.Value <= TimeSpan.Zero ? TimeSpan.Zero : (delay.Value < MaxDelay ? delay.Value : MaxDelay);
    }

    private static HttpRequestMessage Clone(HttpRequestMessage original, byte[]? body)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (body is not null)
        {
            var content = new ByteArrayContent(body);
            if (original.Content?.Headers.ContentType is { } contentType)
            {
                content.Headers.ContentType = contentType;
            }

            clone.Content = content;
        }

        return clone;
    }
}
