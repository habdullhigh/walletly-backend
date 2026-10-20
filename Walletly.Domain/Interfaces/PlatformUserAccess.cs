using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

// Read model returned by GetByIdWithAccessAsync. Permissions are the distinct set granted across Roles.
public sealed record PlatformUserAccess(
    PlatformUser User,
    IReadOnlyCollection<Role> Roles,
    IReadOnlyCollection<Permission> Permissions);
