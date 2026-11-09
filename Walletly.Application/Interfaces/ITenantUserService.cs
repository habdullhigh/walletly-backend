using SharedKernel;
using Walletly.Application.DTOs.Tenants;

namespace Walletly.Application.Interfaces;

public interface ITenantUserService
{
    Task<Result<EnvironmentUserDto>> CreateAsync(Guid environmentId, CreateEnvironmentUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<EnvironmentUserDto>> GetByIdAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EnvironmentUserDto>>> ListAsync(Guid environmentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task<Result<EnvironmentUserDto>> UpdateAsync(Guid environmentId, Guid userId, UpdateEnvironmentUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<EnvironmentUserDto>> AssignRoleAsync(Guid environmentId, Guid userId, AssignUserRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeactivateAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default);
}
