using SharedKernel;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

// The full key looks like wk_{mode}_{keyId}_{secret}. Only the keyId (for lookup) and an Argon2 hash of the secret are stored.
public sealed class TenantApiKey : Entity
{
    private const int MaxNameLength = 100;

    private TenantApiKey()
    {
    }

    private TenantApiKey(Guid id, Guid tenantEnvironmentId, string keyId, string secretHash, string name, DateTime createdAtUtc)
        : base(id)
    {
        TenantEnvironmentId = tenantEnvironmentId;
        KeyId = keyId;
        SecretHash = secretHash;
        Name = name;
        CreatedAt = createdAtUtc;
    }

    public Guid TenantEnvironmentId { get; private set; }
    public string KeyId { get; private set; } = string.Empty;
    public string SecretHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static Result<TenantApiKey> Create(Guid tenantEnvironmentId, string keyId, string secretHash, string name, DateTime createdAtUtc)
    {
        var normalisedName = name.Trim();
        if (normalisedName.Length is 0 or > MaxNameLength)
        {
            return TenantErrors.InvalidApiKeyName;
        }

        return new TenantApiKey(Guid.NewGuid(), tenantEnvironmentId, keyId, secretHash, normalisedName, createdAtUtc);
    }

    public Result Revoke(DateTime revokedAtUtc)
    {
        if (!IsActive)
        {
            return TenantErrors.ApiKeyAlreadyRevoked;
        }

        RevokedAt = revokedAtUtc;
        return Result.Success();
    }
}
