using CRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly AppDbContext _context;

    public EmailVerificationTokenRepository(AppDbContext context) => _context = context;

    public async Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
        => await _context.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default)
        => await _context.EmailVerificationTokens.AddAsync(token, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
