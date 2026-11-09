using SharedKernel;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Application.Services;
using Walletly.Domain.Constants;
using Walletly.Domain.Entities;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Infrastructure.Tests.Tenants;

public class TenantUserAndRoleServiceTests
{
    private readonly FakeTenantRoleRepository _roleRepo = new();
    private readonly FakeTenantUserRepository _userRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly ITenantRoleService _roleService;
    private readonly ITenantUserService _userService;

    public TenantUserAndRoleServiceTests()
    {
        _roleService = new TenantRoleService(_roleRepo, _unitOfWork);
        _userService = new TenantUserService(_userRepo, _roleRepo, _unitOfWork);
    }

    [Fact]
    public async Task CreateRole_WithValidPermissions_Succeeds()
    {
        var envId = Guid.NewGuid();
        var request = new CreateTenantRoleRequest
        {
            Code = "custom_operator",
            Name = "Custom Operator",
            Description = "Can create VAs and receive transfers",
            Permissions = [TenantPermissions.CreateVirtualAccount, TenantPermissions.ReceiveTransfer]
        };

        var result = await _roleService.CreateAsync(envId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("custom_operator", result.Value.Code);
        Assert.Equal(2, result.Value.Permissions.Count);
        Assert.Contains(TenantPermissions.CreateVirtualAccount, result.Value.Permissions);
        Assert.Contains(TenantPermissions.ReceiveTransfer, result.Value.Permissions);
    }

    [Fact]
    public async Task CreateRole_WithInvalidPermission_Fails()
    {
        var envId = Guid.NewGuid();
        var request = new CreateTenantRoleRequest
        {
            Code = "hacker_role",
            Name = "Hacker",
            Permissions = ["invalid.permission.delete_all"]
        };

        var result = await _roleService.CreateAsync(envId, request);

        Assert.True(result.IsFailure);
        Assert.Equal("TenantRoles.InvalidPermission", result.Error.Code);
    }

    [Fact]
    public async Task CreateRole_DuplicateCodeInSameEnvironment_Fails()
    {
        var envId = Guid.NewGuid();
        var request = new CreateTenantRoleRequest
        {
            Code = "finance",
            Name = "Finance Role",
            Permissions = [TenantPermissions.AllowWithdrawals]
        };

        var first = await _roleService.CreateAsync(envId, request);
        Assert.True(first.IsSuccess);

        var duplicate = await _roleService.CreateAsync(envId, request);
        Assert.True(duplicate.IsFailure);
        Assert.Equal(TenantRoleErrors.DuplicateCode, duplicate.Error);
    }

    [Fact]
    public async Task CreateRole_SameCodeInDifferentEnvironments_Succeeds()
    {
        var testEnvId = Guid.NewGuid();
        var liveEnvId = Guid.NewGuid();

        var request = new CreateTenantRoleRequest
        {
            Code = "agent",
            Name = "Agent Role",
            Permissions = [TenantPermissions.MakeTransfer]
        };

        var testResult = await _roleService.CreateAsync(testEnvId, request);
        var liveResult = await _roleService.CreateAsync(liveEnvId, request);

        Assert.True(testResult.IsSuccess);
        Assert.True(liveResult.IsSuccess);
        Assert.NotEqual(testResult.Value.TenantEnvironmentId, liveResult.Value.TenantEnvironmentId);
    }

    [Fact]
    public async Task CreateUser_InEnvironment_Succeeds()
    {
        var envId = Guid.NewGuid();
        var request = new CreateEnvironmentUserRequest
        {
            Email = "customer@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            ExternalReference = "ext-cust-001"
        };

        var result = await _userService.CreateAsync(envId, request);

        Assert.True(result.IsSuccess);
        Assert.Equal("customer@example.com", result.Value.Email);
        Assert.Equal("Alice Smith", result.Value.FullName);
        Assert.Equal(envId, result.Value.TenantEnvironmentId);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task CreateUser_SameEmailInDifferentEnvironments_IsAllowed()
    {
        var testEnvId = Guid.NewGuid();
        var liveEnvId = Guid.NewGuid();

        var request = new CreateEnvironmentUserRequest
        {
            Email = "john.doe@fintech.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var testUser = await _userService.CreateAsync(testEnvId, request);
        var liveUser = await _userService.CreateAsync(liveEnvId, request);

        Assert.True(testUser.IsSuccess);
        Assert.True(liveUser.IsSuccess);
        Assert.NotEqual(testUser.Value.Id, liveUser.Value.Id);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmailInSameEnvironment_Fails()
    {
        var envId = Guid.NewGuid();
        var request = new CreateEnvironmentUserRequest
        {
            Email = "duplicate@fintech.com",
            FirstName = "Jane",
            LastName = "Doe"
        };

        var first = await _userService.CreateAsync(envId, request);
        Assert.True(first.IsSuccess);

        var second = await _userService.CreateAsync(envId, request);
        Assert.True(second.IsFailure);
        Assert.Equal(TenantUserErrors.EmailTaken, second.Error);
    }

    [Fact]
    public async Task AssignRole_AttachesRoleAndPermissionsToUser()
    {
        var envId = Guid.NewGuid();

        var roleResult = await _roleService.CreateAsync(envId, new CreateTenantRoleRequest
        {
            Code = "merchant",
            Name = "Merchant Role",
            Permissions = [TenantPermissions.CreateVirtualAccount, TenantPermissions.ReceiveTransfer]
        });

        var userResult = await _userService.CreateAsync(envId, new CreateEnvironmentUserRequest
        {
            Email = "merchant@store.com",
            FirstName = "Bob",
            LastName = "Merchant"
        });

        var assignResult = await _userService.AssignRoleAsync(envId, userResult.Value.Id, new AssignUserRoleRequest
        {
            RoleId = roleResult.Value.Id
        });

        Assert.True(assignResult.IsSuccess);
        Assert.Equal(roleResult.Value.Id, assignResult.Value.TenantRoleId);
        Assert.Equal("merchant", assignResult.Value.RoleCode);
        Assert.Contains(TenantPermissions.CreateVirtualAccount, assignResult.Value.Permissions);
    }

    [Fact]
    public async Task DeactivateUser_SetsIsActiveFalse()
    {
        var envId = Guid.NewGuid();
        var user = await _userService.CreateAsync(envId, new CreateEnvironmentUserRequest
        {
            Email = "deactivate@example.com",
            FirstName = "Carol",
            LastName = "Danvers"
        });

        var deactResult = await _userService.DeactivateAsync(envId, user.Value.Id);
        Assert.True(deactResult.IsSuccess);

        var fetched = await _userService.GetByIdAsync(envId, user.Value.Id);
        Assert.False(fetched.Value.IsActive);
    }

    // In-memory fakes
    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeTenantRoleRepository : ITenantRoleRepository
    {
        private readonly List<TenantRole> _roles = [];

        public Task<TenantRole?> GetByIdAsync(Guid environmentId, Guid roleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_roles.FirstOrDefault(r => r.TenantEnvironmentId == environmentId && r.Id == roleId));

        public Task<TenantRole?> GetByCodeAsync(Guid environmentId, string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(_roles.FirstOrDefault(r => r.TenantEnvironmentId == environmentId && r.Code == code));

        public Task<IReadOnlyList<TenantRole>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantRole>>(_roles.Where(r => r.TenantEnvironmentId == environmentId).ToList());

        public void Add(TenantRole role) => _roles.Add(role);

        public void Delete(TenantRole role) => _roles.Remove(role);
    }

    private sealed class FakeTenantUserRepository : ITenantUserRepository
    {
        private readonly List<TenantUser> _users = [];

        public Task<TenantUser?> GetByIdAsync(Guid environmentId, Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.TenantEnvironmentId == environmentId && u.Id == userId));

        public Task<TenantUser?> GetByEmailAsync(Guid environmentId, string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.TenantEnvironmentId == environmentId && u.Email == email));

        public Task<TenantUser?> GetByExternalReferenceAsync(Guid environmentId, string externalReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.FirstOrDefault(u => u.TenantEnvironmentId == environmentId && u.ExternalReference == externalReference));

        public Task<IReadOnlyList<TenantUser>> GetByEnvironmentIdAsync(Guid environmentId, int skip = 0, int take = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantUser>>(_users.Where(u => u.TenantEnvironmentId == environmentId).Skip(skip).Take(take).ToList());

        public Task<int> CountByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Count(u => u.TenantEnvironmentId == environmentId));

        public void Add(TenantUser user) => _users.Add(user);

        public void Delete(TenantUser user) => _users.Remove(user);
    }
}
