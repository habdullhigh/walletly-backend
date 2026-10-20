using SharedKernel;
using Walletly.Application.DTOs.Tenants;

namespace Walletly.Application.Interfaces;

public interface ITenantAuthService
{
    Task<Result<TenantAuthResponse>> LoginAsync(LoginTenantUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantUserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
