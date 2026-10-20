using SharedKernel;
using Walletly.Domain.Enums;

namespace Walletly.Domain.Entities;

// One row per tenant per mode (Test and Live). Each environment has its own webhook secret, URL allowlists and API keys.
public sealed class TenantEnvironment : Entity
{
    private TenantEnvironment()
    {
    }

    private TenantEnvironment(Guid id, Guid tenantId, TenantMode mode, string protectedWebhookSecret)
        : base(id)
    {
        TenantId = tenantId;
        Mode = mode;
        ProtectedWebhookSecret = protectedWebhookSecret;
    }

    public Guid TenantId { get; private set; }
    public TenantMode Mode { get; private set; }

    // Encrypted at rest. The plain secret is needed to sign webhook payloads, so it cannot be hashed.
    public string ProtectedWebhookSecret { get; private set; } = string.Empty;

    public static TenantEnvironment Create(Guid tenantId, TenantMode mode, string protectedWebhookSecret) =>
        new(Guid.NewGuid(), tenantId, mode, protectedWebhookSecret);

    public void RotateWebhookSecret(string protectedWebhookSecret) => ProtectedWebhookSecret = protectedWebhookSecret;
}
