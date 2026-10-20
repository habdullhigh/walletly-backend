using SharedKernel;

namespace Walletly.Domain.Errors;

public static class RoleErrors
{
    public static readonly Error InvalidCode = Error.Validation(
        "Roles.InvalidCode",
        "The role code must be lowercase letters, digits, or underscores, and at most 50 characters.");

    public static readonly Error InvalidName = Error.Validation(
        "Roles.InvalidName",
        "The role name is missing or too long.");

    public static readonly Error NotFound = Error.NotFound(
        "Roles.NotFound",
        "The role does not exist.");
}
