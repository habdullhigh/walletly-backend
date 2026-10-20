using SharedKernel;

namespace Walletly.Domain.Errors;

public static class TenantErrors
{
    public static readonly Error InvalidName = Error.Validation(
        "Tenants.InvalidName",
        "The tenant name is missing or too long.");

    public static readonly Error InvalidUrl = Error.Validation(
        "Tenants.InvalidUrl",
        "URLs must be absolute and use HTTPS. Plain HTTP is only allowed for localhost in test mode, and URLs cannot contain credentials or fragments.");

    public static readonly Error InvalidApiKeyName = Error.Validation(
        "Tenants.InvalidApiKeyName",
        "The API key name is missing or too long.");

    public static readonly Error ApiKeyAlreadyRevoked = Error.Conflict(
        "Tenants.ApiKeyAlreadyRevoked",
        "The API key has already been revoked.");

    public static readonly Error NotFound = Error.NotFound(
        "Tenants.NotFound",
        "The tenant does not exist.");

    public static readonly Error EnvironmentNotFound = Error.NotFound(
        "Tenants.EnvironmentNotFound",
        "The tenant environment does not exist.");

    public static readonly Error ApiKeyNotFound = Error.NotFound(
        "Tenants.ApiKeyNotFound",
        "The API key does not exist.");

    public static readonly Error Inactive = Error.Forbidden(
        "Tenants.Inactive",
        "The tenant account is deactivated.");
}
