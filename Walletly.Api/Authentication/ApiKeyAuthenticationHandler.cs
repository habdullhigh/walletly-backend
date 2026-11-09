using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Walletly.Application.Common.Interfaces;
using Walletly.Domain.Interfaces;

namespace Walletly.Api.Authentication;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "TenantApiKey";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ITenantRepository tenantRepository,
    IPasswordHasher passwordHasher)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? rawKey = null;

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerValue = authHeader.ToString().Trim();
            if (headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var candidate = headerValue["Bearer ".Length..].Trim();
                if (candidate.StartsWith("wk_", StringComparison.OrdinalIgnoreCase))
                {
                    rawKey = candidate;
                }
            }
        }

        if (string.IsNullOrEmpty(rawKey) && Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
        {
            rawKey = apiKeyHeader.ToString().Trim();
        }

        if (string.IsNullOrEmpty(rawKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Format: wk_{mode}_{keyId}_{secret}
        var parts = rawKey.Split('_');
        if (parts.Length != 4 || !parts[0].Equals("wk", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Malformed API key format. Expected wk_{mode}_{keyId}_{secret}.");
        }

        var modeStr = parts[1].ToLowerInvariant();
        var keyId = parts[2];
        var secret = parts[3];

        var apiKey = await tenantRepository.GetActiveApiKeyByKeyIdAsync(keyId);
        if (apiKey is null)
        {
            return AuthenticateResult.Fail("Invalid or revoked API key.");
        }

        if (!passwordHasher.VerifyPassword(secret, apiKey.SecretHash))
        {
            return AuthenticateResult.Fail("Invalid API key secret.");
        }

        var environment = await tenantRepository.GetEnvironmentByIdAsync(apiKey.TenantEnvironmentId);
        if (environment is null)
        {
            return AuthenticateResult.Fail("Associated tenant environment not found.");
        }

        if (!environment.Mode.ToString().Equals(modeStr, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("API key mode mismatch.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new("api_key_id", apiKey.Id.ToString()),
            new("api_key_name", apiKey.Name),
            new("tenant_id", environment.TenantId.ToString()),
            new("environment_id", environment.Id.ToString()),
            new("environment_mode", environment.Mode.ToString().ToLowerInvariant()),
            new("mode", environment.Mode.ToString().ToLowerInvariant()),
            new("user_type", "api_key")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
