namespace CRM.Domain.Auth;

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
