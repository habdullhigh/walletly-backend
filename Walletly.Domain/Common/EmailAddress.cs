using System.Text.RegularExpressions;

namespace Walletly.Domain.Common;

public static partial class EmailAddress
{
    public const int MaxLength = 255;

    public static string Normalise(string email) => email.Trim().ToLowerInvariant();

    // Expects a value that has already been through Normalise.
    public static bool IsValid(string normalisedEmail) =>
        normalisedEmail.Length is > 0 and <= MaxLength && EmailPattern().IsMatch(normalisedEmail);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
