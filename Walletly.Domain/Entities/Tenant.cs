using SharedKernel;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed class Tenant : Entity
{
    private const int MaxNameLength = 150;

    private Tenant()
    {
    }

    private Tenant(Guid id, string name, DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        IsActive = true;
        TwoFactorEnabled = false;
        CreatedAt = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Result<Tenant> Create(string name, DateTime createdAtUtc)
    {
        var normalisedName = name.Trim();
        if (normalisedName.Length is 0 or > MaxNameLength)
        {
            return TenantErrors.InvalidName;
        }

        return new Tenant(Guid.NewGuid(), normalisedName, createdAtUtc);
    }

    public void SetTwoFactorEnabled(bool enabled) => TwoFactorEnabled = enabled;

    public void Deactivate() => IsActive = false;
}
