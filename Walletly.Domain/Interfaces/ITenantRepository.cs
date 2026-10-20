using Walletly.Domain.Entities;
using Walletly.Domain.Enums;

namespace Walletly.Domain.Interfaces;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantEnvironment>> GetEnvironmentsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantEnvironment?> GetEnvironmentAsync(Guid tenantId, TenantMode mode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantAllowedUrl>> GetAllowedUrlsAsync(Guid tenantEnvironmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantApiKey>> GetApiKeysAsync(Guid tenantEnvironmentId, CancellationToken cancellationToken = default);
    Task<TenantApiKey?> GetApiKeyAsync(Guid tenantEnvironmentId, Guid apiKeyId, CancellationToken cancellationToken = default);

    // Stage writes only. They reach the database when IUnitOfWork.SaveChangesAsync runs.
    void Add(Tenant tenant);
    void AddEnvironment(TenantEnvironment environment);
    void AddApiKey(TenantApiKey apiKey);

    // Replaces every URL of one kind in one environment. Only the staged rows are changed on SaveChangesAsync.
    Task ReplaceAllowedUrlsAsync(Guid tenantEnvironmentId, AllowedUrlKind kind, IReadOnlyList<TenantAllowedUrl> allowedUrls, CancellationToken cancellationToken = default);
}
