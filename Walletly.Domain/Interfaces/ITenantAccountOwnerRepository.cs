using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

public interface ITenantAccountOwnerRepository
{
    Task<TenantAccountOwner?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<TenantAccountOwner?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TenantAccountOwner?> GetOwnerAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantAccountOwner>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    void Add(TenantAccountOwner owner);
}
