using SharedKernel;

namespace Walletly.Application.Abstractions.Notifications;

public static class NotificationErrors
{
    public static readonly Error InvalidRecipient = Error.Validation(
        "Notifications.InvalidRecipient", "One or more recipient email addresses are invalid.");

    public static readonly Error TemplateNotFound = Error.NotFound(
        "Notifications.TemplateNotFound", "The email template does not exist.");

    public static readonly Error MissingToken = Error.Validation(
        "Notifications.MissingToken", "A token required by the email template was not supplied.");

    public static readonly Error UnknownToken = Error.Validation(
        "Notifications.UnknownToken", "A supplied token is not declared by the email template.");

    public static readonly Error ProviderRejected = Error.Failure(
        "Notifications.ProviderRejected", "The email provider rejected the message.");

    public static readonly Error ProviderUnavailable = Error.Failure(
        "Notifications.ProviderUnavailable", "The email provider could not be reached or is temporarily unavailable.");
}
