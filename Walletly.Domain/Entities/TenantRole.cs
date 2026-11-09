using System.Text.RegularExpressions;
using SharedKernel;
using Walletly.Domain.Constants;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed partial class TenantRole : Entity
{
    private const int MaxCodeLength = 50;
    private const int MaxNameLength = 100;
    private const int MaxDescriptionLength = 500;

    private readonly List<TenantRolePermission> _permissions = [];

    private TenantRole()
    {
    }

    private TenantRole(Guid id, Guid tenantEnvironmentId, string code, string name, string description, DateTime createdAtUtc)
        : base(id)
    {
        TenantEnvironmentId = tenantEnvironmentId;
        Code = code;
        Name = name;
        Description = description;
        CreatedAt = createdAtUtc;
    }

    public Guid TenantEnvironmentId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<TenantRolePermission> Permissions => _permissions.AsReadOnly();

    [GeneratedRegex(@"^[a-z][a-z0-9_]*$")]
    private static partial Regex CodePattern();

    public static Result<TenantRole> Create(
        Guid tenantEnvironmentId,
        string code,
        string name,
        string? description,
        IEnumerable<string>? permissions,
        DateTime createdAtUtc)
    {
        var normalisedCode = code.Trim().ToLowerInvariant();
        if (normalisedCode.Length is 0 or > MaxCodeLength || !CodePattern().IsMatch(normalisedCode))
        {
            return TenantRoleErrors.InvalidCode;
        }

        var normalisedName = name.Trim();
        if (normalisedName.Length is 0 or > MaxNameLength)
        {
            return TenantRoleErrors.InvalidName;
        }

        var cleanDescription = description?.Trim() ?? string.Empty;
        if (cleanDescription.Length > MaxDescriptionLength)
        {
            cleanDescription = cleanDescription[..MaxDescriptionLength];
        }

        var role = new TenantRole(Guid.NewGuid(), tenantEnvironmentId, normalisedCode, normalisedName, cleanDescription, createdAtUtc);

        if (permissions is not null)
        {
            foreach (var permission in permissions)
            {
                var cleanPerm = permission.Trim().ToLowerInvariant();
                if (!TenantPermissions.IsValid(cleanPerm))
                {
                    return TenantRoleErrors.InvalidPermission(permission);
                }

                if (!role._permissions.Any(p => p.Permission == cleanPerm))
                {
                    role._permissions.Add(new TenantRolePermission(role.Id, cleanPerm));
                }
            }
        }

        return role;
    }

    public Result Update(string name, string? description, IEnumerable<string>? permissions)
    {
        var normalisedName = name.Trim();
        if (normalisedName.Length is 0 or > MaxNameLength)
        {
            return TenantRoleErrors.InvalidName;
        }

        Name = normalisedName;
        Description = description?.Trim() ?? string.Empty;

        _permissions.Clear();
        if (permissions is not null)
        {
            foreach (var permission in permissions)
            {
                var cleanPerm = permission.Trim().ToLowerInvariant();
                if (!TenantPermissions.IsValid(cleanPerm))
                {
                    return TenantRoleErrors.InvalidPermission(permission);
                }

                if (!_permissions.Any(p => p.Permission == cleanPerm))
                {
                    _permissions.Add(new TenantRolePermission(Id, cleanPerm));
                }
            }
        }

        return Result.Success();
    }
}
