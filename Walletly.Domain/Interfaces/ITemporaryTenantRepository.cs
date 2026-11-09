using Walletly.Domain.Entities;

namespace Walletly.Domain.Interfaces;

public interface ITemporaryTenantRepository
{
    Task<TemporaryTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Newest row that is still waiting: a waitlist entry or a pending verification.
    Task<TemporaryTenant?> GetOpenByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<TemporaryTenant?> GetByInvitationTokenHashAsync(string invitationTokenHash, CancellationToken cancellationToken = default);

    // Stage writes only. They reach the database when IUnitOfWork.SaveChangesAsync runs.
    void Add(TemporaryTenant temporaryTenant);
    void Remove(TemporaryTenant temporaryTenant);
}
