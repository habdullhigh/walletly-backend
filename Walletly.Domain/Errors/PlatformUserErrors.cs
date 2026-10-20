using SharedKernel;

namespace Walletly.Domain.Errors;

public static class PlatformUserErrors
{
    public static readonly Error InvalidEmail = Error.Validation(
        "PlatformUsers.InvalidEmail",
        "The email address is missing, malformed, or too long.");

    public static readonly Error InvalidFirstName = Error.Validation(
        "PlatformUsers.InvalidFirstName",
        "The first name is missing or too long.");

    public static readonly Error InvalidLastName = Error.Validation(
        "PlatformUsers.InvalidLastName",
        "The last name is missing or too long.");

    public static readonly Error InvalidPasswordHash = Error.Validation(
        "PlatformUsers.InvalidPasswordHash",
        "A password hash is required.");

    public static readonly Error NotFound = Error.NotFound(
        "PlatformUsers.NotFound",
        "The platform user does not exist.");

    public static readonly Error EmailTaken = Error.Conflict(
        "PlatformUsers.EmailTaken",
        "A platform user with this email already exists.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "PlatformUsers.InvalidCredentials",
        "Invalid email or password.");

    public static readonly Error Inactive = Error.Forbidden(
        "PlatformUsers.Inactive",
        "The platform user account is deactivated.");
}
