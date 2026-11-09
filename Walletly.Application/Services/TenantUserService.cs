using SharedKernel;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Entities;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

public class TenantUserService(
    ITenantUserRepository userRepository,
    ITenantRoleRepository roleRepository,
    IUnitOfWork unitOfWork) : ITenantUserService
{
    public async Task<Result<EnvironmentUserDto>> CreateAsync(Guid environmentId, CreateEnvironmentUserRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByEmailAsync(environmentId, request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (existing is not null)
        {
            return TenantUserErrors.EmailTaken;
        }

        TenantRole? role = null;
        if (request.RoleId.HasValue)
        {
            role = await roleRepository.GetByIdAsync(environmentId, request.RoleId.Value, cancellationToken);
            if (role is null)
            {
                return TenantRoleErrors.RoleNotFound;
            }
        }

        var userResult = TenantUser.Create(
            environmentId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.RoleId,
            request.ExternalReference,
            DateTime.UtcNow);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        var user = userResult.Value;
        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user, role);
    }

    public async Task<Result<EnvironmentUserDto>> GetByIdAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(environmentId, userId, cancellationToken);
        if (user is null)
        {
            return TenantUserErrors.NotFound;
        }

        TenantRole? role = null;
        if (user.TenantRoleId.HasValue)
        {
            role = await roleRepository.GetByIdAsync(environmentId, user.TenantRoleId.Value, cancellationToken);
        }

        return ToDto(user, role);
    }

    public async Task<Result<IReadOnlyList<EnvironmentUserDto>>> ListAsync(Guid environmentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetByEnvironmentIdAsync(environmentId, skip, take, cancellationToken);
        var roles = await roleRepository.GetByEnvironmentIdAsync(environmentId, cancellationToken);
        var roleMap = roles.ToDictionary(r => r.Id);

        var dtos = users.Select(u =>
        {
            TenantRole? role = null;
            if (u.TenantRoleId.HasValue)
            {
                roleMap.TryGetValue(u.TenantRoleId.Value, out role);
            }

            return ToDto(u, role);
        }).ToList();

        return dtos;
    }

    public async Task<Result<EnvironmentUserDto>> UpdateAsync(Guid environmentId, Guid userId, UpdateEnvironmentUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(environmentId, userId, cancellationToken);
        if (user is null)
        {
            return TenantUserErrors.NotFound;
        }

        var updateResult = user.UpdateDetails(request.FirstName, request.LastName, request.ExternalReference);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        TenantRole? role = null;
        if (user.TenantRoleId.HasValue)
        {
            role = await roleRepository.GetByIdAsync(environmentId, user.TenantRoleId.Value, cancellationToken);
        }

        return ToDto(user, role);
    }

    public async Task<Result<EnvironmentUserDto>> AssignRoleAsync(Guid environmentId, Guid userId, AssignUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(environmentId, userId, cancellationToken);
        if (user is null)
        {
            return TenantUserErrors.NotFound;
        }

        TenantRole? role = null;
        if (request.RoleId.HasValue)
        {
            role = await roleRepository.GetByIdAsync(environmentId, request.RoleId.Value, cancellationToken);
            if (role is null)
            {
                return TenantRoleErrors.RoleNotFound;
            }
        }

        user.AssignRole(request.RoleId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user, role);
    }

    public async Task<Result> DeactivateAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(environmentId, userId, cancellationToken);
        if (user is null)
        {
            return TenantUserErrors.NotFound;
        }

        user.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static EnvironmentUserDto ToDto(TenantUser user, TenantRole? role) => new()
    {
        Id = user.Id,
        TenantEnvironmentId = user.TenantEnvironmentId,
        TenantRoleId = user.TenantRoleId,
        RoleCode = role?.Code,
        RoleName = role?.Name,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        ExternalReference = user.ExternalReference,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        Permissions = role?.Permissions.Select(p => p.Permission).ToList() ?? []
    };
}
