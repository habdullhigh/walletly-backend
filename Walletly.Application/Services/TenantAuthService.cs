using SharedKernel;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Entities;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

public class TenantAuthService : ITenantAuthService
{
    private const int TokenLifetimeSeconds = 3600 * 24; // 24 hours

    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantTokenGenerator _tenantTokenGenerator;

    public TenantAuthService(
        ITenantUserRepository tenantUserRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITenantTokenGenerator tenantTokenGenerator)
    {
        _tenantUserRepository = tenantUserRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tenantTokenGenerator = tenantTokenGenerator;
    }

    public async Task<Result<TenantAuthResponse>> LoginAsync(LoginTenantUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _tenantUserRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return TenantUserErrors.InvalidCredentials;
        }

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return TenantErrors.Inactive;
        }

        if (tenant.TwoFactorEnabled)
        {
            return TenantUserErrors.TwoFactorRequired;
        }

        var loginResult = user.RecordLogin(DateTime.UtcNow);
        if (loginResult.IsFailure)
        {
            return loginResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TenantAuthResponse
        {
            AccessToken = _tenantTokenGenerator.GenerateToken(user),
            ExpiresIn = TokenLifetimeSeconds,
            User = ToDto(user)
        };
    }

    public async Task<Result<TenantUserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _tenantUserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return TenantUserErrors.NotFound;
        }

        return ToDto(user);
    }

    private static TenantUserDto ToDto(TenantUser user) => new()
    {
        Id = user.Id,
        TenantId = user.TenantId,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}
