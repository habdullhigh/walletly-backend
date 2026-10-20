using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedKernel;
using Walletly.Application.Abstractions.Notifications;

namespace Walletly.Infrastructure.Notifications.Providers;

public sealed class BrevoEmailProvider(HttpClient httpClient) : IEmailProvider
{
    // Keyed-service name. Matches Email:Provider "Brevo" once lowercased.
    public const string Key = "brevo";

    public async Task<Result<EmailSendResult>> SendAsync(RenderedEmail email, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync("smtp/email", ToRequest(email), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return NotificationErrors.ProviderUnavailable;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout from the HttpClient, not from the caller.
            return NotificationErrors.ProviderUnavailable;
        }

        using (response)
        {
            // Any 2xx means Brevo accepted the message. Treating a 200 as a rejection would fail a job whose mail was sent.
            if (response.IsSuccessStatusCode)
            {
                return await ReadMessageIdAsync(response, cancellationToken);
            }

            // 429 and 5xx are temporary. Other 4xx mean the request itself is wrong, so retrying will not help.
            // Response bodies are not copied into the error, since they can echo recipient addresses.
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
            {
                return NotificationErrors.ProviderUnavailable;
            }

            return NotificationErrors.ProviderRejected;
        }
    }

    private static async Task<EmailSendResult> ReadMessageIdAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

            var messageId = document.RootElement.TryGetProperty("messageId", out var property)
                ? property.GetString() ?? string.Empty
                : string.Empty;

            return new EmailSendResult(messageId);
        }
        catch (JsonException)
        {
            // The message was accepted, so an unreadable body must not turn into a retry that could send it twice.
            return new EmailSendResult(string.Empty);
        }
    }

    private static BrevoSendRequest ToRequest(RenderedEmail email) => new(
        Sender: ToContact(email.Sender),
        To: email.To.Select(ToContact).ToList(),
        Subject: email.Subject,
        HtmlContent: email.HtmlContent,
        Cc: email.Cc?.Select(ToContact).ToList(),
        Bcc: email.Bcc?.Select(ToContact).ToList(),
        ReplyTo: email.ReplyTo is null ? null : ToContact(email.ReplyTo),
        Attachment: email.Attachments?.Select(attachment => new BrevoAttachment(attachment.FileName, attachment.ContentBase64)).ToList(),
        Tags: email.Tags);

    private static BrevoContact ToContact(EmailAddressee addressee) => new(addressee.Email, addressee.Name);

    private sealed record BrevoSendRequest(
        BrevoContact Sender,
        IReadOnlyList<BrevoContact> To,
        string Subject,
        string HtmlContent,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BrevoContact>? Cc,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BrevoContact>? Bcc,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] BrevoContact? ReplyTo,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<BrevoAttachment>? Attachment,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<string>? Tags);

    private sealed record BrevoContact(
        string Email,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Name);

    private sealed record BrevoAttachment(string Name, string Content);
}
