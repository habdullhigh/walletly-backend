using SharedKernel;

namespace Walletly.Domain.Errors;

public static class TenantUserErrors
{
    public static readonly Error InvalidEmail = Error.Validation(
        "TenantUsers.InvalidEmail",
        "The email address is missing, malformed, or too long.");

    public static readonly Error InvalidFirstName = Error.Validation(
        "TenantUsers.InvalidFirstName",
        "The first name is missing or too long.");

    public static readonly Error InvalidLastName = Error.Validation(
        "TenantUsers.InvalidLastName",
        "The last name is missing or too long.");

    public static readonly Error WeakPassword = Error.Validation(
        "TenantUsers.WeakPassword",
        "The password must be at least 8 characters.");

    public static readonly Error InvalidPasswordHash = Error.Validation(
        "TenantUsers.InvalidPasswordHash",
        "A password hash is required.");

    public static readonly Error NotFound = Error.NotFound(
        "TenantUsers.NotFound",
        "The tenant user does not exist.");

    public static readonly Error EmailTaken = Error.Conflict(
        "TenantUsers.EmailTaken",
        "A tenant user with this email already exists.");

    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "TenantUsers.InvalidCredentials",
        "Invalid email or password.");

    public static readonly Error Inactive = Error.Forbidden(
        "TenantUsers.Inactive",
        "The tenant user account is deactivated.");

    // Fails closed: tenants with 2FA enabled cannot log in until the 2FA flow exists.
    public static readonly Error TwoFactorRequired = Error.Forbidden(
        "TenantUsers.TwoFactorRequired",
        "This tenant requires two-factor authentication, which is not available yet.");
}
