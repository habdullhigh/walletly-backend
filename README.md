# Walletly backend

Wallet-as-a-Service MVP for embedding NGN fiat wallets and payments. .NET 10 modular monolith, DDD, CQRS without MediatR. See [CLAUDE.md](CLAUDE.md) for the repo conventions.

## Build and test

```bash
dotnet build Walletly.slnx
dotnet test Walletly.slnx
```

The Hangfire storage test uses Testcontainers, so Docker must be running.

## Configuration and secrets

Secrets come only from environment variables, user secrets, or a secret store. `appsettings*.json` holds placeholders only.

| Setting | Environment variable | Notes |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | PostgreSQL. Also backs Hangfire storage. |
| `Email:Brevo:ApiKey` | `Email__Brevo__ApiKey` | Secret. Never commit a real key. |
| `Email:SenderEmail` | `Email__SenderEmail` | Must be a sender verified in Brevo (Senders & IP). Brevo rejects unverified senders. |
| `Email:SenderName` | `Email__SenderName` | Display name for outgoing mail. |
| `Email:Provider` | `Email__Provider` | `Brevo` is the only supported value today. |
| `Email:Brevo:BaseUrl` | `Email__Brevo__BaseUrl` | Defaults to `https://api.brevo.com/v3`. |
| `Email:Brevo:TimeoutSeconds` | `Email__Brevo__TimeoutSeconds` | Defaults to 30. |

For local development, use user secrets on the API project:

```bash
dotnet user-secrets set "Email:Brevo:ApiKey" "<your-brevo-api-key>" --project Walletly.Api
dotnet user-secrets set "Email:SenderEmail" "<verified-sender@yourdomain.com>" --project Walletly.Api
```

The app validates email settings at startup. An unknown `Email:Provider`, or a missing key or sender, stops the app with a message that names the setting. It never prints the key.

## Background jobs

Hangfire runs on PostgreSQL storage (`Hangfire.PostgreSql`), using the same `DefaultConnection`. Hangfire creates its own tables on first start.

**Do not ship an in-memory Hangfire store.** Queued emails, including OTP emails, would be lost on every restart and would not be shared between instances. Any shared or production environment must use the PostgreSQL storage configured in `BackgroundJobsServiceCollectionExtensions`.

Hangfire stores job arguments, including OTP token values, until the job finishes. The dashboard and tables are therefore sensitive. Restrict database access, and do not expose the Hangfire dashboard without authorization.

## Email notifications

Callers send email through `IEmailService` (in `Walletly.Application.Abstractions.Notifications`):

```csharp
var result = await emailService.EnqueueAsync(new EmailMessage(
    TemplateKey: "otp-verification",
    To: [new EmailAddressee(email, name)],
    Tokens: new Dictionary<string, string>
    {
        ["first_name"] = name,
        ["otp_code"] = otp,
        ["expires_in_minutes"] = "10"
    }));
```

- `EnqueueAsync` returns the outcome of queuing only. It validates the template, the tokens, and the recipients, then queues a Hangfire job. It does not wait for the provider.
- The job renders the template, sends through the active provider, and records the outcome in logs.
- Transient failures (429, 5xx, timeouts, connection errors) are retried with backoff: 1, 5, 15, and 60 minutes, five attempts in total. Permanent failures (other 4xx) fail the job without retry.
- Templates live in `Walletly.Infrastructure/Notifications/Templates/` as embedded resources. Each template key has a `{key}.html` body and a `{key}.subject.txt` subject. Tokens are written as `{{token_name}}`. The template declares its own tokens. A missing token returns `Notifications.MissingToken`, and an undeclared token returns `Notifications.UnknownToken`. Nothing is sent in either case. Body values are HTML-encoded.
- Message bodies, tokens, and OTP codes are never logged.

### Adding a new provider

Suppose you add a provider named `Mailgun`:

1. **Implement `IEmailProvider`** in `Walletly.Infrastructure/Notifications/Providers/`. Return `NotificationErrors.ProviderUnavailable` for transient failures (429, 5xx, timeouts) and `NotificationErrors.ProviderRejected` for permanent ones. Never throw for an expected failure, and never put the response body into an error.
2. **Add an options class** in `Walletly.Infrastructure/Notifications/Configuration/` (for example `MailgunOptions`). Give it a `SectionName` such as `"Email:Mailgun"`, with `[Required]` attributes on its secret fields.
3. **Register it as a keyed service** in `NotificationsServiceCollectionExtensions.AddEmailNotifications`. Use a typed `HttpClient` with a timeout, and add the new options binding and `ValidateDataAnnotations` as needed. The key is the provider name in lowercase, for example `"mailgun"`.
4. **Add config:** add the section to `appsettings.json` with empty placeholders, and document the environment variable names in the table above. Then add the provider name to `EmailOptionsValidator`, so an unknown name stops startup and a missing section is reported.

Adding a provider needs no change to callers or to `EmailSendJob`. The job resolves the active provider through `IEmailProviderSelector`, which reads `Email:Provider`.

## Project layout

- `SharedKernel`: `Result`, `Error`, `IDateTimeProvider`. No dependencies.
- `Walletly.Domain`: entities and domain logic.
- `Walletly.Application`: use cases and abstractions, including `IEmailService`.
- `Walletly.Infrastructure`: EF Core, Hangfire, and the email providers and templates.
- `Walletly.Api`: HTTP only. `Program.cs` is the composition root.
- `Walletly.Infrastructure.Tests`: unit tests, plus a Testcontainers integration test for Hangfire storage.
