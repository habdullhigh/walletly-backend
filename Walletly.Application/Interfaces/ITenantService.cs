using SharedKernel;
using Walletly.Application.DTOs.Tenants;
using Walletly.Domain.Enums;

namespace Walletly.Application.Interfaces;

public interface ITenantService
{
    Task<Result<TenantDto>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> UpdateSettingsAsync(Guid tenantId, UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default);
    Task<Result<TenantDto>> UpdateAllowedUrlsAsync(Guid tenantId, TenantMode mode, UpdateAllowedUrlsRequest request, CancellationToken cancellationToken = default);
    Task<Result<RotatedWebhookSecretDto>> RotateWebhookSecretAsync(Guid tenantId, TenantMode mode, CancellationToken cancellationToken = default);
    Task<Result<CreatedApiKeyDto>> CreateApiKeyAsync(Guid tenantId, TenantMode mode, CreateApiKeyRequest request, CancellationToken cancellationToken = default);
    Task<Result<List<ApiKeyDto>>> ListApiKeysAsync(Guid tenantId, TenantMode mode, CancellationToken cancellationToken = default);
    Task<Result> RevokeApiKeyAsync(Guid tenantId, TenantMode mode, Guid apiKeyId, CancellationToken cancellationToken = default);
}
