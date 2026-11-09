namespace Walletly.Domain.Enums;

// PendingApproval tenants can use sandbox only. Live mode opens once a platform admin activates the tenant.
public enum TenantStatus
{
    PendingApproval = 1,
    Active = 2
}
