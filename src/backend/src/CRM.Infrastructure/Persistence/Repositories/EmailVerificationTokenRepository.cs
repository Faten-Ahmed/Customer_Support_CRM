using CRM.Domain.Customers;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub until the email verification token storage (US-BE-014) is implemented.
public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    public Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
        => Task.FromResult<EmailVerificationToken?>(null);

    public Task AddAsync(EmailVerificationToken token, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
