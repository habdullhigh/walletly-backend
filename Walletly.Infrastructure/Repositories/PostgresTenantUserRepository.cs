using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories;

public class PostgresTenantUserRepository(WalletlyDbContext context) : ITenantUserRepository
{
    public Task<TenantUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.TenantUsers.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<TenantUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.TenantUsers.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public void Add(TenantUser user) => context.TenantUsers.Add(user);
}
