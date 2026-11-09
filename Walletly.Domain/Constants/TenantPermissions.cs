namespace Walletly.Domain.Constants;

public static class TenantPermissions
{
    public const string CreateVirtualAccount = "virtual_accounts.create";
    public const string ReceiveTransfer = "transfers.receive";
    public const string MakeTransfer = "transfers.send";
    public const string AllowWithdrawals = "withdrawals.allow";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        CreateVirtualAccount,
        ReceiveTransfer,
        MakeTransfer,
        AllowWithdrawals
    };

    public static bool IsValid(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && All.Contains(permission.Trim());
}
