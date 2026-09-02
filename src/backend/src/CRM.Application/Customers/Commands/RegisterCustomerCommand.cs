using System.Security.Cryptography;
using System.Text;
using CRM.Application.Common;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record RegisterCustomerCommand(
    string FullName,
    string FullNameAr,
    string Email,
    string Password) : IRequest;

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerCredentialRepository _credentials;
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly IEmailService _email;

    public RegisterCustomerCommandHandler(
        ICustomerRepository customers,
        ICustomerCredentialRepository credentials,
        IEmailVerificationTokenRepository tokens,
        IEmailService email)
    {
        _customers = customers;
        _credentials = credentials;
        _tokens = tokens;
        _email = email;
    }

    public async Task Handle(RegisterCustomerCommand cmd, CancellationToken ct)
    {
        var existing = await _customers.FindByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Email '{cmd.Email}' is already registered.");

        var customer = Customer.Create(cmd.FullName, cmd.FullNameAr, cmd.Email, null, null);

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password);
        var credential = CustomerCredential.Create(customer.Id, passwordHash);

        // 6-digit OTP — short enough to type, long enough for a 24-hour window
        var rawToken = Random.Shared.Next(100_000, 1_000_000).ToString();
        var tokenHash = HashString(rawToken);
        var verificationToken = EmailVerificationToken.Create(customer.Id, tokenHash);

        await _customers.AddAsync(customer, ct);
        await _credentials.AddAsync(credential, ct);
        await _tokens.AddAsync(verificationToken, ct);

        await _customers.SaveChangesAsync(ct);
        await _credentials.SaveChangesAsync(ct);
        await _tokens.SaveChangesAsync(ct);

        await _email.SendVerificationEmailAsync(cmd.Email, cmd.FullName, rawToken, ct);
    }

    private static string HashString(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
