using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging;
using SharedKernel;
using Walletly.Infrastructure.Notifications.Providers;

namespace Walletly.Infrastructure.Tests.Notifications;

internal sealed class FakeBackgroundJobClient : IBackgroundJobClient
{
    public List<(Job Job, IState State)> Created { get; } = [];

    public string Create(Job job, IState state)
    {
        Created.Add((job, state));
        return Created.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;
}

internal sealed class FakeEmailProvider(Func<RenderedEmail, Result<EmailSendResult>> respond) : IEmailProvider
{
    public List<RenderedEmail> Sent { get; } = [];

    public Task<Result<EmailSendResult>> SendAsync(RenderedEmail email, CancellationToken cancellationToken)
    {
        Sent.Add(email);
        return Task.FromResult(respond(email));
    }
}

internal sealed class FakeProviderSelector(IEmailProvider provider) : IEmailProviderSelector
{
    public IEmailProvider Resolve() => provider;
}

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return respond(request);
    }
}

internal sealed class FixedDateTimeProvider(DateTime utcNow) : SharedKernel.IDateTimeProvider
{
    public DateTime UtcNow => utcNow;
}

// Collects formatted log text so tests can assert on what was written.
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var text = formatter(state, exception);
            provider.Messages.Add(exception is null ? text : $"{text} {exception}");
        }
    }
}
