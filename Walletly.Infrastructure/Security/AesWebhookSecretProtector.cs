using System.Security.Cryptography;
using System.Text;
using Walletly.Application.Common.Interfaces;

namespace Walletly.Infrastructure.Security;

// AES-256-GCM. Output format: "v1:" + base64(nonce || tag || ciphertext).
public class AesWebhookSecretProtector : IWebhookSecretProtector
{
    private const string Version = "v1:";
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesWebhookSecretProtector(WebhookSecretSettings settings)
    {
        _key = Convert.FromBase64String(settings.EncryptionKey);
        if (_key.Length != KeySize)
        {
            throw new InvalidOperationException("WebhookSecrets:EncryptionKey must be a base64-encoded 256-bit key.");
        }
    }

    public string Protect(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, NonceSize);
        ciphertext.CopyTo(payload, NonceSize + TagSize);

        return Version + Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        if (!protectedValue.StartsWith(Version, StringComparison.Ordinal))
        {
            throw new CryptographicException("Unsupported webhook secret format.");
        }

        var payload = Convert.FromBase64String(protectedValue[Version.Length..]);
        var nonce = payload[..NonceSize];
        var tag = payload[NonceSize..(NonceSize + TagSize)];
        var ciphertext = payload[(NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
