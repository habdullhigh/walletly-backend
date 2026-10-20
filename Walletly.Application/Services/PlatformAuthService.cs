using SharedKernel;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.DTOs.Auth;
using Walletly.Application.Interfaces;
using Walletly.Domain.Entities;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

public class PlatformAuthService : IPlatformAuthService
{
    private const int TokenLifetimeSeconds = 3600 * 24; // 24 hours

    private readonly IPlatformUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public PlatformAuthService(
        IPlatformUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<PlatformUserDto>> RegisterAsync(RegisterPlatformUserRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return PlatformUserErrors.EmailTaken;
        }

        var roleCode = string.IsNullOrWhiteSpace(request.RoleName) ? "developer" : request.RoleName.Trim().ToLowerInvariant();
        var role = await _userRepository.GetRoleByCodeAsync(roleCode, cancellationToken);
        if (role is null)
        {
            return RoleErrors.NotFound;
        }

        var userResult = PlatformUser.Create(
            email,
            _passwordHasher.HashPassword(request.Password),
            request.FirstName,
            request.LastName,
            DateTime.UtcNow);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;
        _userRepository.Add(user);
        _userRepository.AssignRole(new PlatformUserRole(user.Id, role.Id));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var access = await _userRepository.GetByIdWithAccessAsync(user.Id, cancellationToken);
        if (access is null)
        {
            return PlatformUserErrors.NotFound;
        }

        return ToDto(access);
    }

    public async Task<Result<PlatformAuthResponse>> LoginAsync(LoginPlatformUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return PlatformUserErrors.InvalidCredentials;
        }

        var loginResult = user.RecordLogin(DateTime.UtcNow);
        if (loginResult.IsFailure)
        {
            return loginResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueTokenAsync(user.Id, cancellationToken);
    }

    public async Task<Result<PlatformUserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var access = await _userRepository.GetByIdWithAccessAsync(userId, cancellationToken);
        if (access is null)
        {
            return PlatformUserErrors.NotFound;
        }

        return ToDto(access);
    }

    private async Task<Result<PlatformAuthResponse>> IssueTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var access = await _userRepository.GetByIdWithAccessAsync(userId, cancellationToken);
        if (access is null)
        {
            return PlatformUserErrors.NotFound;
        }

        var dto = ToDto(access);
        var token = _jwtTokenGenerator.GenerateToken(access.User, dto.Roles, dto.Permissions);

        return new PlatformAuthResponse
        {
            AccessToken = token,
            ExpiresIn = TokenLifetimeSeconds,
            User = dto
        };
    }

    private static PlatformUserDto ToDto(PlatformUserAccess access) => new()
    {
        Id = access.User.Id,
        Email = access.User.Email,
        FirstName = access.User.FirstName,
        LastName = access.User.LastName,
        FullName = access.User.FullName,
        IsActive = access.User.IsActive,
        CreatedAt = access.User.CreatedAt,
        Roles = access.Roles.Select(r => r.Code).ToList(),
        Permissions = access.Permissions.Select(p => p.Code).ToList()
    };
}
