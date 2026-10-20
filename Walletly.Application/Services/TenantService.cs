using System.Security.Cryptography;
using SharedKernel;
using Walletly.Application.Common.Interfaces;
using Walletly.Application.DTOs.Tenants;
using Walletly.Application.Interfaces;
using Walletly.Domain.Entities;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;
using Walletly.Domain.Interfaces;

namespace Walletly.Application.Services;

public class TenantService : ITenantService
{
    private const int MinPasswordLength = 8;
    private const string ApiKeyPrefix = "wk";
    private const string WebhookSecretPrefix = "whsec_";

    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebhookSecretProtector _webhookSecretProtector;

    public TenantService(
        ITenantRepository tenantRepository,
        ITenantUserRepository tenantUserRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IWebhookSecretProtector webhookSecretProtector)
    {
        _tenantRepository = tenantRepository;
        _tenantUserRepository = tenantUserRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _webhookSecretProtector = webhookSecretProtector;
    }

    public async Task<Result<TenantDto>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var ownerEmail = request.OwnerEmail.Trim().ToLowerInvariant();
        if (await _tenantUserRepository.GetByEmailAsync(ownerEmail, cancellationToken) is not null)
        {
            return TenantUserErrors.EmailTaken;
        }

        if (request.OwnerPassword.Length < MinPasswordLength)
        {
            return TenantUserErrors.WeakPassword;
        }

        var now = DateTime.UtcNow;

        var tenantResult = Tenant.Create(request.Name, now);
        if (tenantResult.IsFailure)
        {
            return tenantResult.Error;
        }

        var tenant = tenantResult.Value;

        var ownerResult = TenantUser.Create(
            tenant.Id,
            ownerEmail,
            _passwordHasher.HashPassword(request.OwnerPassword),
            request.OwnerFirstName,
            request.OwnerLastName,
            now);

        if (ownerResult.IsFailure)
        {
            return ownerResult.Error;
        }

        _tenantRepository.Add(tenant);

        // Every tenant gets both environments up front, so test and live keys can coexist from day one.
        foreach (var mode in Enum.GetValues<TenantMode>())
        {
            _tenantRepository.AddEnvironment(TenantEnvironment.Create(tenant.Id, mode, _webhookSecretProtector.Protect(GenerateWebhookSecret())));
        }

        _tenantUserRepository.Add(ownerResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(tenant, cancellationToken);
    }

    public async Task<Result<TenantDto>> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound;
        }

        return await BuildDtoAsync(tenant, cancellationToken);
    }

    public async Task<Result<TenantDto>> UpdateSettingsAsync(Guid tenantId, UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return TenantErrors.NotFound;
        }

        tenant.SetTwoFactorEnabled(request.TwoFactorEnabled);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(tenant, cancellationToken);
    }

    public async Task<Result<TenantDto>> UpdateAllowedUrlsAsync(Guid tenantId, TenantMode mode, UpdateAllowedUrlsRequest request, CancellationToken cancellationToken = default)
    {
        var environment = await _tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (environment is null)
        {
            return TenantErrors.EnvironmentNotFound;
        }

        // Validate both lists before touching the database, so a bad entry changes nothing.
        var webhookUrls = BuildAllowedUrls(environment.Id, mode, AllowedUrlKind.Webhook, request.WebhookUrls);
        if (webhookUrls.IsFailure)
        {
            return webhookUrls.Error;
        }

        var returnUrls = BuildAllowedUrls(environment.Id, mode, AllowedUrlKind.Return, request.ReturnUrls);
        if (returnUrls.IsFailure)
        {
            return returnUrls.Error;
        }

        await _tenantRepository.ReplaceAllowedUrlsAsync(environment.Id, AllowedUrlKind.Webhook, webhookUrls.Value, cancellationToken);
        await _tenantRepository.ReplaceAllowedUrlsAsync(environment.Id, AllowedUrlKind.Return, returnUrls.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(tenantId, cancellationToken);
    }

    public async Task<Result<RotatedWebhookSecretDto>> RotateWebhookSecretAsync(Guid tenantId, TenantMode mode, CancellationToken cancellationToken = default)
    {
        var environment = await _tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (environment is null)
        {
            return TenantErrors.EnvironmentNotFound;
        }

        var secret = GenerateWebhookSecret();
        environment.RotateWebhookSecret(_webhookSecretProtector.Protect(secret));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RotatedWebhookSecretDto
        {
            Mode = ModeName(mode),
            WebhookSecret = secret
        };
    }

    public async Task<Result<CreatedApiKeyDto>> CreateApiKeyAsync(Guid tenantId, TenantMode mode, CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        var environment = await _tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (environment is null)
        {
            return TenantErrors.EnvironmentNotFound;
        }

        // keyId is public and used for lookup. The secret is shown once and stored only as an Argon2 hash.
        var keyId = RandomHex(12);
        var secret = RandomHex(32);

        var apiKeyResult = TenantApiKey.Create(
            environment.Id,
            keyId,
            _passwordHasher.HashPassword(secret),
            request.Name,
            DateTime.UtcNow);

        if (apiKeyResult.IsFailure)
        {
            return apiKeyResult.Error;
        }

        _tenantRepository.AddApiKey(apiKeyResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreatedApiKeyDto
        {
            ApiKey = ToApiKeyDto(apiKeyResult.Value, mode),
            Key = $"{KeyPrefix(mode, keyId)}_{secret}"
        };
    }

    public async Task<Result<List<ApiKeyDto>>> ListApiKeysAsync(Guid tenantId, TenantMode mode, CancellationToken cancellationToken = default)
    {
        var environment = await _tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (environment is null)
        {
            return TenantErrors.EnvironmentNotFound;
        }

        var apiKeys = await _tenantRepository.GetApiKeysAsync(environment.Id, cancellationToken);
        return apiKeys.Select(k => ToApiKeyDto(k, mode)).ToList();
    }

    public async Task<Result> RevokeApiKeyAsync(Guid tenantId, TenantMode mode, Guid apiKeyId, CancellationToken cancellationToken = default)
    {
        var environment = await _tenantRepository.GetEnvironmentAsync(tenantId, mode, cancellationToken);
        if (environment is null)
        {
            return TenantErrors.EnvironmentNotFound;
        }

        var apiKey = await _tenantRepository.GetApiKeyAsync(environment.Id, apiKeyId, cancellationToken);
        if (apiKey is null)
        {
            return TenantErrors.ApiKeyNotFound;
        }

        var revokeResult = apiKey.Revoke(DateTime.UtcNow);
        if (revokeResult.IsFailure)
        {
            return revokeResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<TenantDto> BuildDtoAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var environments = await _tenantRepository.GetEnvironmentsAsync(tenant.Id, cancellationToken);

        var environmentDtos = new List<TenantEnvironmentDto>();
        foreach (var environment in environments.OrderBy(e => e.Mode))
        {
            var urls = await _tenantRepository.GetAllowedUrlsAsync(environment.Id, cancellationToken);

            environmentDtos.Add(new TenantEnvironmentDto
            {
                Mode = ModeName(environment.Mode),
                WebhookUrls = urls.Where(u => u.Kind == AllowedUrlKind.Webhook).Select(u => u.Url).ToList(),
                ReturnUrls = urls.Where(u => u.Kind == AllowedUrlKind.Return).Select(u => u.Url).ToList()
            });
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            IsActive = tenant.IsActive,
            TwoFactorEnabled = tenant.TwoFactorEnabled,
            CreatedAt = tenant.CreatedAt,
            Environments = environmentDtos
        };
    }

    private static Result<IReadOnlyList<TenantAllowedUrl>> BuildAllowedUrls(Guid environmentId, TenantMode mode, AllowedUrlKind kind, IEnumerable<string> urls)
    {
        var allowedUrls = new List<TenantAllowedUrl>();
        foreach (var url in urls.Select(u => u.Trim()).Distinct(StringComparer.Ordinal))
        {
            var allowedUrlResult = TenantAllowedUrl.Create(environmentId, mode, kind, url);
            if (allowedUrlResult.IsFailure)
            {
                return allowedUrlResult.Error;
            }

            allowedUrls.Add(allowedUrlResult.Value);
        }

        return allowedUrls;
    }

    private static ApiKeyDto ToApiKeyDto(TenantApiKey apiKey, TenantMode mode) => new()
    {
        Id = apiKey.Id,
        Name = apiKey.Name,
        Mode = ModeName(mode),
        KeyPrefix = KeyPrefix(mode, apiKey.KeyId),
        CreatedAt = apiKey.CreatedAt,
        RevokedAt = apiKey.RevokedAt
    };

    private static string KeyPrefix(TenantMode mode, string keyId) => $"{ApiKeyPrefix}_{ModeName(mode)}_{keyId}";

    private static string ModeName(TenantMode mode) => mode.ToString().ToLowerInvariant();

    private static string GenerateWebhookSecret() => WebhookSecretPrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string RandomHex(int byteCount) => Convert.ToHexString(RandomNumberGenerator.GetBytes(byteCount)).ToLowerInvariant();
}
