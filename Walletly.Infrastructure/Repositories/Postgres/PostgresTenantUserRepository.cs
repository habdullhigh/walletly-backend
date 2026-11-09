using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories.Postgres;

public class PostgresTenantUserRepository(WalletlyDbContext context) : ITenantUserRepository
{
    public Task<TenantUser?> GetByIdAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default) =>
        context.TenantUsers.FirstOrDefaultAsync(u => u.TenantEnvironmentId == environmentId && u.Id == userId, cancellationToken);

    public Task<TenantUser?> GetByEmailAsync(Guid environmentId, string email, CancellationToken cancellationToken = default) =>
        context.TenantUsers.FirstOrDefaultAsync(u => u.TenantEnvironmentId == environmentId && u.Email == email, cancellationToken);

    public Task<TenantUser?> GetByExternalReferenceAsync(Guid environmentId, string externalReference, CancellationToken cancellationToken = default) =>
        context.TenantUsers.FirstOrDefaultAsync(u => u.TenantEnvironmentId == environmentId && u.ExternalReference == externalReference, cancellationToken);

    public async Task<IReadOnlyList<TenantUser>> GetByEnvironmentIdAsync(Guid environmentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default) =>
        await context.TenantUsers
            .Where(u => u.TenantEnvironmentId == environmentId)
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken = default) =>
        context.TenantUsers.CountAsync(u => u.TenantEnvironmentId == environmentId, cancellationToken);

    public void Add(TenantUser user) => context.TenantUsers.Add(user);

    public void Delete(TenantUser user) => context.TenantUsers.Remove(user);
}
