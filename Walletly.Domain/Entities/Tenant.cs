using SharedKernel;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed class Tenant : Entity
{
    private const int MaxNameLength = 150;

    private Tenant()
    {
    }

    private Tenant(Guid id, string name, TenantStatus status, DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        Status = status;
        IsActive = true;
        TwoFactorEnabled = false;
        CreatedAt = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public bool IsActive { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Live keys and live transactions need platform approval. Sandbox works in every status.
    public bool IsLiveModeEnabled => Status == TenantStatus.Active;

    public static Result<Tenant> Create(string name, DateTime createdAtUtc) =>
        CreateWithStatus(name, TenantStatus.Active, createdAtUtc);

    public static Result<Tenant> CreatePendingApproval(string name, DateTime createdAtUtc) =>
        CreateWithStatus(name, TenantStatus.PendingApproval, createdAtUtc);

    public Result Activate()
    {
        if (Status == TenantStatus.Active)
        {
            return TenantErrors.AlreadyActive;
        }

        Status = TenantStatus.Active;
        return Result.Success();
    }

    public void SetTwoFactorEnabled(bool enabled) => TwoFactorEnabled = enabled;

    public void Deactivate() => IsActive = false;

    private static Result<Tenant> CreateWithStatus(string name, TenantStatus status, DateTime createdAtUtc)
    {
        var normalisedName = name.Trim();
        if (normalisedName.Length is 0 or > MaxNameLength)
        {
            return TenantErrors.InvalidName;
        }

        return new Tenant(Guid.NewGuid(), normalisedName, status, createdAtUtc);
    }
}
