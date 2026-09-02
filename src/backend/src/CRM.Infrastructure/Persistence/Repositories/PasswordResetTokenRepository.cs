using CRM.Domain.Auth;
using CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _db;

    public PasswordResetTokenRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
        => await _db.PasswordResetTokens.AddAsync(token, ct);

    public Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
