using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

public interface IPlatformUserRepository
{
    Task<PlatformUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<PlatformUserAccess?> GetByIdWithAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByCodeAsync(string roleCode, CancellationToken cancellationToken = default);

    // Stage writes only. They reach the database when IUnitOfWork.SaveChangesAsync runs.
    void Add(PlatformUser user);
    void AssignRole(PlatformUserRole assignment);
}
