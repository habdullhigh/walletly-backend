using System.Security.Cryptography;

namespace Walletly.Application.Common;

public static class WebhookSecrets
{
    private const string Prefix = "whsec_";

    public static string Generate() => Prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
