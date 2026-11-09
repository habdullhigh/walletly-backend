using SharedKernel;

namespace Walletly.Domain.Errors;

public static class TenantRoleErrors
{
    public static readonly Error InvalidCode = Error.Validation(
        "TenantRoles.InvalidCode",
        "The role code must be lowercase letters, digits, or underscores, and at most 50 characters.");

    public static readonly Error InvalidName = Error.Validation(
        "TenantRoles.InvalidName",
        "The role name is missing or too long.");

    public static readonly Error RoleNotFound = Error.NotFound(
        "TenantRoles.NotFound",
        "The specified role was not found in this environment.");

    public static readonly Error DuplicateCode = Error.Conflict(
        "TenantRoles.DuplicateCode",
        "A role with this code already exists in this environment.");

    public static Error InvalidPermission(string permission) => Error.Validation(
        "TenantRoles.InvalidPermission",
        $"The permission '{permission}' is not a recognized system permission.");
}
