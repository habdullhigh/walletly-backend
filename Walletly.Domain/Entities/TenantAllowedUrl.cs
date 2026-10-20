using SharedKernel;
using Walletly.Domain.Enums;
using Walletly.Domain.Errors;

namespace Walletly.Domain.Entities;

public sealed class TenantAllowedUrl : Entity
{
    private const int MaxUrlLength = 2048;

    private TenantAllowedUrl()
    {
    }

    private TenantAllowedUrl(Guid id, Guid tenantEnvironmentId, AllowedUrlKind kind, string url)
        : base(id)
    {
        TenantEnvironmentId = tenantEnvironmentId;
        Kind = kind;
        Url = url;
    }

    public Guid TenantEnvironmentId { get; private set; }
    public AllowedUrlKind Kind { get; private set; }
    public string Url { get; private set; } = string.Empty;

    // HTTPS is required. Plain HTTP is allowed only for localhost in the Test environment.
    public static Result<TenantAllowedUrl> Create(Guid tenantEnvironmentId, TenantMode mode, AllowedUrlKind kind, string url)
    {
        var candidate = url.Trim();
        if (candidate.Length is 0 or > MaxUrlLength || !Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return TenantErrors.InvalidUrl;
        }

        var isLocalhost = uri.Host is "localhost" or "127.0.0.1";
        var schemeAllowed = uri.Scheme == Uri.UriSchemeHttps
            || (uri.Scheme == Uri.UriSchemeHttp && isLocalhost && mode == TenantMode.Test);

        if (!schemeAllowed || uri.UserInfo.Length > 0 || uri.Fragment.Length > 0)
        {
            return TenantErrors.InvalidUrl;
        }

        return new TenantAllowedUrl(Guid.NewGuid(), tenantEnvironmentId, kind, candidate);
    }
}
