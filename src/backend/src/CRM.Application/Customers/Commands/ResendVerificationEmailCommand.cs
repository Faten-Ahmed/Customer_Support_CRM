using System.Security.Cryptography;
using System.Text;
using CRM.Application.Common;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record ResendVerificationEmailCommand(string Email) : IRequest;

public class ResendVerificationEmailCommandHandler : IRequestHandler<ResendVerificationEmailCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerCredentialRepository _credentials;
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly IEmailService _email;

    public ResendVerificationEmailCommandHandler(
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

    public async Task Handle(ResendVerificationEmailCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByEmailAsync(cmd.Email, ct);
        if (customer is null)
            return; // don't reveal whether the email exists

        var credential = await _credentials.FindByCustomerIdAsync(customer.Id, ct);
        if (credential is null || credential.EmailVerified)
            return; // already verified or no credential — nothing to do

        await _tokens.DeleteUnusedByCustomerIdAsync(customer.Id, ct);
        await _tokens.SaveChangesAsync(ct);

        var rawToken = Random.Shared.Next(100_000, 1_000_000).ToString();
        var tokenHash = HashString(rawToken);
        var verificationToken = EmailVerificationToken.Create(customer.Id, tokenHash);

        await _tokens.AddAsync(verificationToken, ct);
        await _tokens.SaveChangesAsync(ct);

        await _email.SendVerificationEmailAsync(cmd.Email, customer.FullName, rawToken, ct);
    }

    private static string HashString(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
