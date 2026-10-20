using SharedKernel;
using Walletly.Application.DTOs.Auth;

namespace Walletly.Application.Interfaces;

public interface IPlatformAuthService
{
    Task<Result<PlatformUserDto>> RegisterAsync(RegisterPlatformUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<PlatformAuthResponse>> LoginAsync(LoginPlatformUserRequest request, CancellationToken cancellationToken = default);
    Task<Result<PlatformUserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
