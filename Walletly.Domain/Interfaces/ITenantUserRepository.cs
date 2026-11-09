using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

public interface ITenantUserRepository
{
    Task<TenantUser?> GetByIdAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<TenantUser?> GetByEmailAsync(Guid environmentId, string email, CancellationToken cancellationToken = default);
    Task<TenantUser?> GetByExternalReferenceAsync(Guid environmentId, string externalReference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantUser>> GetByEnvironmentIdAsync(Guid environmentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<int> CountByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken = default);

    // Stage writes only. They reach the database when IUnitOfWork.SaveChangesAsync runs.
    void Add(TenantUser user);
    void Delete(TenantUser user);
}
