namespace Walletly.Application.Common.Interfaces;

// Reversible encryption for webhook signing secrets. They must be recoverable to sign payloads, so they cannot be hashed.
public interface IWebhookSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
