namespace Walletly.Domain.Entities;

public sealed class PlatformUserRole
{
    private PlatformUserRole()
    {
    }

    public PlatformUserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
}
