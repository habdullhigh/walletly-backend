using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories.Postgres;

public class PostgresTenantRoleRepository(WalletlyDbContext context) : ITenantRoleRepository
{
    public Task<TenantRole?> GetByIdAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default) =>
        context.TenantRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.TenantEnvironmentId == environmentId && r.Id == roleId, cancellationToken);

    public Task<TenantRole?> GetByCodeAsync(Guid environmentId, string code, CancellationToken cancellationToken = default) =>
        context.TenantRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.TenantEnvironmentId == environmentId && r.Code == code, cancellationToken);

    public async Task<IReadOnlyList<TenantRole>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken = default) =>
        await context.TenantRoles
            .Include(r => r.Permissions)
            .Where(r => r.TenantEnvironmentId == environmentId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

    public void Add(TenantRole role) => context.TenantRoles.Add(role);

    public void Delete(TenantRole role) => context.TenantRoles.Remove(role);
}
