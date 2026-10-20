using SharedKernel;

namespace Walletly.Domain.Errors;

public static class PermissionErrors
{
    public static readonly Error InvalidCode = Error.Validation(
        "Permissions.InvalidCode",
        "The permission code must be dot-separated lowercase segments, such as platform.users.create.");
}
