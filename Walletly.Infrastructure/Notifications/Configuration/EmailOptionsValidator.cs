using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Walletly.Infrastructure.Notifications.Configuration;

internal sealed class EmailOptionsValidator(IOptions<BrevoOptions> brevoOptions) : IValidateOptions<EmailOptions>
{
    private const string BrevoEmailProviderName = "Brevo";

    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (!string.Equals(options.Provider, BrevoEmailProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"Email:Provider '{options.Provider}' is not supported. Supported providers: {BrevoEmailProviderName}.");
        }

        var brevo = brevoOptions.Value;
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(brevo, new ValidationContext(brevo), results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Success;
        }

        // Member names only. Error text never includes the configured key.
        var failures = results.Select(result => $"{BrevoOptions.SectionName}: {result.ErrorMessage}");
        return ValidateOptionsResult.Fail(failures);
    }
}
