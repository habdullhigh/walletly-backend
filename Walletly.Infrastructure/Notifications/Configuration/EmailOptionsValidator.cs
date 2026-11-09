using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Walletly.Infrastructure.Notifications.Configuration;

internal sealed class EmailOptionsValidator(
    IOptions<BrevoOptions> brevoOptions,
    IOptions<SmtpOptions> smtpOptions) : IValidateOptions<EmailOptions>
{
    private const string BrevoEmailProviderName = "Brevo";
    private const string SmtpEmailProviderName = "Smtp";

    private static readonly string SupportedProviders =
        $"{BrevoEmailProviderName}, {SmtpEmailProviderName}";

    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        if (string.Equals(options.Provider, BrevoEmailProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateSection(brevoOptions.Value, BrevoOptions.SectionName);
        }

        if (string.Equals(options.Provider, SmtpEmailProviderName, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateSection(smtpOptions.Value, SmtpOptions.SectionName);
        }

        return ValidateOptionsResult.Fail(
            $"Email:Provider '{options.Provider}' is not supported. Supported providers: {SupportedProviders}.");
    }

    private static ValidateOptionsResult ValidateSection(object sectionOptions, string sectionName)
    {
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(sectionOptions, new ValidationContext(sectionOptions), results, validateAllProperties: true))
        {
            return ValidateOptionsResult.Success;
        }

        // Member names only. Error text never includes configured secrets.
        var failures = results.Select(result => $"{sectionName}: {result.ErrorMessage}");
        return ValidateOptionsResult.Fail(failures);
    }
}
