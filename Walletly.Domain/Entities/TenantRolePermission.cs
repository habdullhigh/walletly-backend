namespace Walletly.Domain.Entities;

public sealed class TenantRolePermission
{
    private TenantRolePermission()
    {
    }

    public TenantRolePermission(Guid roleId, string permission)
    {
        RoleId = roleId;
        Permission = permission;
    }

    public Guid RoleId { get; private set; }
    public string Permission { get; private set; } = string.Empty;
}
