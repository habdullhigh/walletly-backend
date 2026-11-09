using Microsoft.EntityFrameworkCore;
using Walletly.Domain.Entities;
using Walletly.Domain.Enums;
using Walletly.Domain.Interfaces;
using Walletly.Infrastructure.Data;

namespace Walletly.Infrastructure.Repositories.Postgres;

public class PostgresTemporaryTenantRepository(WalletlyDbContext context) : ITemporaryTenantRepository
{
    public Task<TemporaryTenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.TemporaryTenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TemporaryTenant?> GetOpenByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        context.TemporaryTenants
            .Where(t => t.Email == email
                && (t.Status == TemporaryTenantStatus.WaitlistPending || t.Status == TemporaryTenantStatus.PendingVerification))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<TemporaryTenant?> GetByInvitationTokenHashAsync(string invitationTokenHash, CancellationToken cancellationToken = default) =>
        context.TemporaryTenants.FirstOrDefaultAsync(t => t.InvitationTokenHash == invitationTokenHash, cancellationToken);

    public void Add(TemporaryTenant temporaryTenant) => context.TemporaryTenants.Add(temporaryTenant);

    public void Remove(TemporaryTenant temporaryTenant) => context.TemporaryTenants.Remove(temporaryTenant);
}
