using SharedKernel;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Constants;
using Walletly.Domain.Entities;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

public class TenantRoleService(
    ITenantRoleRepository roleRepository,
    IUnitOfWork unitOfWork) : ITenantRoleService
{
    public async Task<Result<TenantRoleDto>> CreateAsync(Guid environmentId, CreateTenantRoleRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await roleRepository.GetByCodeAsync(environmentId, request.Code, cancellationToken);
        if (existing is not null)
        {
            return TenantRoleErrors.DuplicateCode;
        }

        var roleResult = TenantRole.Create(
            environmentId,
            request.Code,
            request.Name,
            request.Description,
            request.Permissions,
            DateTime.UtcNow);

        if (roleResult.IsFailure)
        {
            return roleResult.Error;
        }

        var role = roleResult.Value;
        roleRepository.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(role);
    }

    public async Task<Result<TenantRoleDto>> GetByIdAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetByIdAsync(environmentId, roleId, cancellationToken);
        if (role is null)
        {
            return TenantRoleErrors.RoleNotFound;
        }

        return ToDto(role);
    }

    public async Task<Result<IReadOnlyList<TenantRoleDto>>> ListAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var roles = await roleRepository.GetByEnvironmentIdAsync(environmentId, cancellationToken);
        return roles.Select(ToDto).ToList();
    }

    public async Task<Result<TenantRoleDto>> UpdateAsync(Guid environmentId, Guid roleId, UpdateTenantRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetByIdAsync(environmentId, roleId, cancellationToken);
        if (role is null)
        {
            return TenantRoleErrors.RoleNotFound;
        }

        var updateResult = role.Update(request.Name, request.Description, request.Permissions);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(role);
    }

    public async Task<Result> DeleteAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await roleRepository.GetByIdAsync(environmentId, roleId, cancellationToken);
        if (role is null)
        {
            return TenantRoleErrors.RoleNotFound;
        }

        roleRepository.Delete(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public IReadOnlySet<string> GetAvailablePermissions() => TenantPermissions.All;

    private static TenantRoleDto ToDto(TenantRole role) => new()
    {
        Id = role.Id,
        TenantEnvironmentId = role.TenantEnvironmentId,
        Code = role.Code,
        Name = role.Name,
        Description = role.Description,
        CreatedAt = role.CreatedAt,
        Permissions = role.Permissions.Select(p => p.Permission).ToList()
    };
}
