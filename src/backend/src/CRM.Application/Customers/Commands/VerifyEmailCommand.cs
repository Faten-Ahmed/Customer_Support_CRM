// src/CRM.Application/Customers/Commands/VerifyEmailCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly ICustomerCredentialRepository _credentials;

    public VerifyEmailCommandHandler(
        IEmailVerificationTokenRepository tokens,
        ICustomerCredentialRepository credentials)
    {
        _tokens = tokens;
        _credentials = credentials;
    }

    public async Task Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var tokenHash = HashString(cmd.Token);

        var token = await _tokens.FindByHashAsync(tokenHash, ct)
            ?? throw new KeyNotFoundException("Verification token not found.");

        if (!token.IsValid)
            throw new InvalidOperationException("Token has expired or already been used.");

        token.MarkUsed();

        var credential = await _credentials.FindByCustomerIdAsync(token.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Credential for customer {token.CustomerId} not found.");

        credential.VerifyEmail();

        await _tokens.SaveChangesAsync(ct);
        await _credentials.SaveChangesAsync(ct);
    }

    private static string HashString(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
