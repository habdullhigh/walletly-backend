using Walletly.Application.Common.Interfaces;
using Walletly.Application.DTOs.Tenants;
using Walletly.Domain.Entities;

namespace Walletly.Application.Common;

public static class TenantResponses
{
    public const int TokenLifetimeSeconds = 3600 * 24; // 24 hours

    public static TenantAuthResponse ToAuthResponse(TenantAccountOwner owner, ITenantTokenGenerator tokenGenerator) => new()
    {
        AccessToken = tokenGenerator.GenerateToken(owner),
        ExpiresIn = TokenLifetimeSeconds,
        User = ToUserDto(owner)
    };

    public static TenantUserDto ToUserDto(TenantAccountOwner owner) => new()
    {
        Id = owner.Id,
        TenantId = owner.TenantId,
        Email = owner.Email,
        FirstName = owner.FirstName,
        LastName = owner.LastName,
        FullName = owner.FullName,
        Role = owner.Role.ToString().ToLowerInvariant(),
        IsActive = owner.IsActive,
        CreatedAt = owner.CreatedAt
    };
}
