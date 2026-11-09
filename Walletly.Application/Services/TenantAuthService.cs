using SharedKernel;
using Walletly.Application.Common;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

public class TenantAuthService : ITenantAuthService
{
    private readonly ITenantAccountOwnerRepository _accountOwnerRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantTokenGenerator _tenantTokenGenerator;

    public TenantAuthService(
        ITenantAccountOwnerRepository accountOwnerRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITenantTokenGenerator tenantTokenGenerator)
    {
        _accountOwnerRepository = accountOwnerRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tenantTokenGenerator = tenantTokenGenerator;
    }

    public async Task<Result<TenantAuthResponse>> LoginAsync(LoginTenantUserRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await _accountOwnerRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (owner is null || !_passwordHasher.VerifyPassword(request.Password, owner.PasswordHash))
        {
            return TenantUserErrors.InvalidCredentials;
        }

        var tenant = await _tenantRepository.GetByIdAsync(owner.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return TenantErrors.Inactive;
        }

        if (tenant.TwoFactorEnabled)
        {
            return TenantUserErrors.TwoFactorRequired;
        }

        var loginResult = owner.RecordLogin(DateTime.UtcNow);
        if (loginResult.IsFailure)
        {
            return loginResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TenantResponses.ToAuthResponse(owner, _tenantTokenGenerator);
    }

    public async Task<Result<TenantUserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var owner = await _accountOwnerRepository.GetByIdAsync(userId, cancellationToken);
        if (owner is null)
        {
            return TenantUserErrors.NotFound;
        }

        return TenantResponses.ToUserDto(owner);
    }
}
