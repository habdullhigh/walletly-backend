using Walletly.Domain.Entities;

namespace Walletly.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(PlatformUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
}
