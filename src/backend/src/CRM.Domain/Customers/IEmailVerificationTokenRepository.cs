// src/CRM.Domain/Customers/IEmailVerificationTokenRepository.cs
namespace CRM.Domain.Customers;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
