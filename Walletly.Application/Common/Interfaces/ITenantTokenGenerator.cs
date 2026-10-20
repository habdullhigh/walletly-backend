using Walletly.Domain.Entities;

namespace Walletly.Application.Common.Interfaces;

public interface ITenantTokenGenerator
{
    string GenerateToken(TenantUser user);
}
