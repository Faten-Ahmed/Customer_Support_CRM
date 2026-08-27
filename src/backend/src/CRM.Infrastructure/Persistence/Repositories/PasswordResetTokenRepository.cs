using CRM.Domain.Auth;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub until the PasswordResetTokens EF table is added.
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
        => Task.FromResult<PasswordResetToken?>(null);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
