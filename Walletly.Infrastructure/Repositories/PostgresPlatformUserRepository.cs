using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories;

public class PostgresPlatformUserRepository(WalletlyDbContext context) : IPlatformUserRepository
{
    public Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.PlatformUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<PlatformUserAccess?> GetByIdWithAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.PlatformUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var roleIds = context.PlatformUserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId);

        var roles = await context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var permissions = await context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Join(context.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new PlatformUserAccess(user, roles, permissions);
    }

    public Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default) =>
        context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode, cancellationToken);

    public void Add(PlatformUser user) => context.PlatformUsers.Add(user);

    public void AssignRole(PlatformUserRole assignment) => context.PlatformUserRoles.Add(assignment);
}
