using System.Net;
using System.Text.Json;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;
using Walletly.Infrastructure.Notifications.Providers;

namespace Walletly.Infrastructure.Tests.Notifications;

public sealed class BrevoEmailProviderTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SendAsync_MapsRequest_ToBrevoContract()
    {
        var handler = new StubHttpMessageHandler(_ => Created("<message-1@smtp-relay>"));
        var provider = CreateProvider(handler);

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("<message-1@smtp-relay>", result.Value.ProviderMessageId);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", request.RequestUri!.ToString());
        Assert.Equal("test-key", request.Headers.GetValues("api-key").Single());

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("noreply@walletly.test", root.GetProperty("sender").GetProperty("email").GetString());
        Assert.Equal("Walletly", root.GetProperty("sender").GetProperty("name").GetString());
        Assert.Equal("ada@example.com", root.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("Ada", root.GetProperty("to")[0].GetProperty("name").GetString());
        Assert.Equal("Your code", root.GetProperty("subject").GetString());
        Assert.Equal("<p>482913</p>", root.GetProperty("htmlContent").GetString());
        Assert.Equal("support@walletly.test", root.GetProperty("replyTo").GetProperty("email").GetString());
        Assert.Equal("statement.pdf", root.GetProperty("attachment")[0].GetProperty("name").GetString());
        Assert.Equal("aGVsbG8=", root.GetProperty("attachment")[0].GetProperty("content").GetString());
        Assert.Equal("otp", root.GetProperty("tags")[0].GetString());
        Assert.False(root.TryGetProperty("cc", out _));
        Assert.False(root.TryGetProperty("bcc", out _));
    }

    [Fact]
    public async Task Response201_IsSuccess()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => Created("<id@brevo>")));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Response200_IsSuccess_LikeTheExistingPhpClient()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, """{"messageId":"<ok@brevo>"}""")));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Response201WithUnreadableBody_IsStillSuccess_SoTheMailIsNotSentTwice()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => Json(HttpStatusCode.Created, "not json")));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Response400_IsPermanent_ProviderRejected()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => Json(HttpStatusCode.BadRequest, """{"message":"Invalid email ada@example.com"}""")));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(NotificationErrors.ProviderRejected, result.Error);
        Assert.DoesNotContain("ada@example.com", result.Error.Description);
    }

    [Fact]
    public async Task Response429_IsTransient_ProviderUnavailable()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.Equal(NotificationErrors.ProviderUnavailable, result.Error);
    }

    [Fact]
    public async Task Response500_IsTransient_ProviderUnavailable()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.Equal(NotificationErrors.ProviderUnavailable, result.Error);
    }

    [Fact]
    public async Task ConnectionFailure_IsTransient_ProviderUnavailable()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(_ => throw new HttpRequestException("connection reset")));

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.Equal(NotificationErrors.ProviderUnavailable, result.Error);
    }

    [Fact]
    public async Task RetryAfter_OnFirst429_IsHonored_AndBodyIsResent()
    {
        var responses = new Queue<HttpResponseMessage>();
        var rateLimited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        rateLimited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
        responses.Enqueue(rateLimited);
        responses.Enqueue(Created("<retried@brevo>"));

        var stub = new StubHttpMessageHandler(_ => responses.Dequeue());
        var retryHandler = new RetryAfterDelegatingHandler(new FixedDateTimeProvider(FixedNow)) { InnerHandler = stub };
        var provider = CreateProvider(retryHandler);

        var result = await provider.SendAsync(SampleEmail(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, stub.Requests.Count);
        Assert.Equal(stub.Requests[0].Body, stub.Requests[1].Body);
    }

    private static BrevoEmailProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.brevo.com/v3/")
        };
        httpClient.DefaultRequestHeaders.Add("api-key", "test-key");
        return new BrevoEmailProvider(httpClient);
    }

    private static HttpResponseMessage Created(string messageId) =>
        Json(HttpStatusCode.Created, $$"""{"messageId":"{{messageId}}"}""");

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) => new(statusCode)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
    };

    private static RenderedEmail SampleEmail() => new(
        Sender: new EmailAddressee("noreply@walletly.test", "Walletly"),
        To: [new EmailAddressee("ada@example.com", "Ada")],
        Subject: "Your code",
        HtmlContent: "<p>482913</p>",
        ReplyTo: new EmailAddressee("support@walletly.test"),
        Attachments: [new EmailAttachment("statement.pdf", "aGVsbG8=")],
        Tags: ["otp"]);
}
