using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories.Postgres;

public class PostgresTenantAccountOwnerRepository(WalletlyDbContext context) : ITenantAccountOwnerRepository
{
    public Task<TenantAccountOwner?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.TenantAccountOwners.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<TenantAccountOwner?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.TenantAccountOwners.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<TenantAccountOwner?> GetOwnerAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        context.TenantAccountOwners
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantAccountOwner>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await context.TenantAccountOwners
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(TenantAccountOwner owner) => context.TenantAccountOwners.Add(owner);
}
