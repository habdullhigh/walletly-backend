using System.Collections.Concurrent;
using Walletly.Application.Common.Interfaces;
using Walletly.Domain.Entities;
using Walletly.Domain.Interfaces;

namespace Walletly.Infrastructure.Repositories;

public class InMemoryPlatformUserRepository : IPlatformUserRepository
{
    private readonly ConcurrentDictionary<Guid, PlatformUser> _users = new();
    private readonly ConcurrentDictionary<Guid, Role> _roles = new();
    private readonly ConcurrentDictionary<Guid, Permission> _permissions = new();
    private readonly ConcurrentDictionary<(Guid UserId, Guid RoleId), PlatformUserRole> _userRoles = new();
    private readonly ConcurrentDictionary<(Guid RoleId, Guid PermissionId), RolePermission> _rolePermissions = new();

    private readonly IPasswordHasher _passwordHasher;

    public InMemoryPlatformUserRepository(IPasswordHasher? passwordHasher = null)
    {
        _passwordHasher = passwordHasher ?? new Security.Argon2PasswordHasher();
        SeedDefaultRolesAndPermissions();
        SeedDefaultAdmin();
    }

    private void SeedDefaultAdmin()
    {
        var adminRole = _roles.Values.FirstOrDefault(r => r.Code == "admin");
        if (adminRole is null) return;

        var email = "walletly.admin@yopmail.com";
        var passwordHash = _passwordHasher.HashPassword("AdminPassword123!");
        var user = PlatformUser.Create(email, passwordHash, "Platform", "Administrator", DateTime.UtcNow).Value;

        _users[user.Id] = user;
        _userRoles[(user.Id, adminRole.Id)] = new PlatformUserRole(user.Id, adminRole.Id);
    }

    private void SeedDefaultRolesAndPermissions()
    {
        var permTenantRead = Permission.Create("tenants.read").Value;
        var permTenantWrite = Permission.Create("tenants.write").Value;
        var permWalletRead = Permission.Create("wallets.read").Value;
        var permWalletWrite = Permission.Create("wallets.write").Value;
        var permUsersManage = Permission.Create("platform_users.manage").Value;
        var permRolesManage = Permission.Create("roles.manage").Value;

        foreach (var p in new[] { permTenantRead, permTenantWrite, permWalletRead, permWalletWrite, permUsersManage, permRolesManage })
        {
            _permissions[p.Id] = p;
        }

        var adminRole = Role.Create("admin", "Admin").Value;
        var devRole = Role.Create("developer", "Developer").Value;
        var supportRole = Role.Create("support", "Support").Value;

        foreach (var role in new[] { adminRole, devRole, supportRole })
        {
            _roles[role.Id] = role;
        }

        foreach (var p in new[] { permTenantRead, permTenantWrite, permWalletRead, permWalletWrite, permUsersManage, permRolesManage })
        {
            _rolePermissions[(adminRole.Id, p.Id)] = new RolePermission(adminRole.Id, p.Id);
        }

        foreach (var p in new[] { permTenantRead, permWalletRead })
        {
            _rolePermissions[(devRole.Id, p.Id)] = new RolePermission(devRole.Id, p.Id);
        }

        _rolePermissions[(supportRole.Id, permTenantRead.Id)] = new RolePermission(supportRole.Id, permTenantRead.Id);
    }

    public Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u => u.Email == email);
        return Task.FromResult(user);
    }

    public Task<PlatformUserAccess?> GetByIdWithAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(userId, out var user))
        {
            return Task.FromResult<PlatformUserAccess?>(null);
        }

        var roleIds = _userRoles.Keys.Where(k => k.UserId == userId).Select(k => k.RoleId).ToHashSet();
        var roles = _roles.Values.Where(r => roleIds.Contains(r.Id)).ToList();

        var permissionIds = _rolePermissions.Keys
            .Where(k => roleIds.Contains(k.RoleId))
            .Select(k => k.PermissionId)
            .ToHashSet();
        var permissions = _permissions.Values.Where(p => permissionIds.Contains(p.Id)).ToList();

        return Task.FromResult<PlatformUserAccess?>(new PlatformUserAccess(user, roles, permissions));
    }

    public Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        var role = _roles.Values.FirstOrDefault(r => r.Code == roleCode);
        return Task.FromResult(role);
    }

    public void Add(PlatformUser user) => _users[user.Id] = user;

    public void AssignRole(PlatformUserRole assignment) =>
        _userRoles[(assignment.UserId, assignment.RoleId)] = assignment;
}
