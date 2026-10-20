using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

public interface ITenantUserRepository
{
    Task<TenantUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<TenantUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    // Stage writes only. They reach the database when IUnitOfWork.SaveChangesAsync runs.
    void Add(TenantUser user);
}
