using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Enums;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories;

public class PostgresTenantRepository(WalletlyDbContext context) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

    public async Task<IReadOnlyList<TenantEnvironment>> GetEnvironmentsAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await context.TenantEnvironments
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public Task<TenantEnvironment?> GetEnvironmentAsync(Guid tenantId, TenantMode mode, CancellationToken cancellationToken = default) =>
        context.TenantEnvironments.FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Mode == mode, cancellationToken);

    public async Task<IReadOnlyList<TenantAllowedUrl>> GetAllowedUrlsAsync(Guid tenantEnvironmentId, CancellationToken cancellationToken = default) =>
        await context.TenantAllowedUrls
            .Where(u => u.TenantEnvironmentId == tenantEnvironmentId)
            .OrderBy(u => u.Url)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantApiKey>> GetApiKeysAsync(Guid tenantEnvironmentId, CancellationToken cancellationToken = default) =>
        await context.TenantApiKeys
            .Where(k => k.TenantEnvironmentId == tenantEnvironmentId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<TenantApiKey?> GetApiKeyAsync(Guid tenantEnvironmentId, Guid apiKeyId, CancellationToken cancellationToken = default) =>
        context.TenantApiKeys.FirstOrDefaultAsync(k => k.TenantEnvironmentId == tenantEnvironmentId && k.Id == apiKeyId, cancellationToken);

    public void Add(Tenant tenant) => context.Tenants.Add(tenant);

    public void AddEnvironment(TenantEnvironment environment) => context.TenantEnvironments.Add(environment);

    public void AddApiKey(TenantApiKey apiKey) => context.TenantApiKeys.Add(apiKey);

    public async Task ReplaceAllowedUrlsAsync(Guid tenantEnvironmentId, AllowedUrlKind kind, IReadOnlyList<TenantAllowedUrl> allowedUrls, CancellationToken cancellationToken = default)
    {
        var existing = await context.TenantAllowedUrls
            .Where(u => u.TenantEnvironmentId == tenantEnvironmentId && u.Kind == kind)
            .ToListAsync(cancellationToken);

        var desired = allowedUrls.Select(u => u.Url).ToHashSet(StringComparer.Ordinal);
        var current = existing.Select(u => u.Url).ToHashSet(StringComparer.Ordinal);

        // Write only the difference. Re-inserting a URL that is kept would clash with the unique index.
        context.TenantAllowedUrls.RemoveRange(existing.Where(u => !desired.Contains(u.Url)));
        context.TenantAllowedUrls.AddRange(allowedUrls.Where(u => !current.Contains(u.Url)));
    }
}
