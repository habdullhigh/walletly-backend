using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

public interface ITenantRoleRepository
{
    Task<TenantRole?> GetByIdAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default);
    Task<TenantRole?> GetByCodeAsync(Guid environmentId, string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantRole>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken = default);
    void Add(TenantRole role);
    void Delete(TenantRole role);
}
