using SharedKernel;
using Walletly.Application.DTOs.Tenants;

namespace Walletly.Application.Interfaces;

public interface ITenantRoleService
{
    Task<Result<TenantRoleDto>> CreateAsync(Guid environmentId, CreateTenantRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantRoleDto>> GetByIdAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TenantRoleDto>>> ListAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<Result<TenantRoleDto>> UpdateAsync(Guid environmentId, Guid roleId, UpdateTenantRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default);
    IReadOnlySet<string> GetAvailablePermissions();
}
