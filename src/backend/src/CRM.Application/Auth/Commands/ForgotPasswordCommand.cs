using System.Security.Cryptography;
using System.Text;
using CRM.Application.Common;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IEmailService _email;

    public ForgotPasswordCommandHandler(
        IUserRepository users,
        IPasswordResetTokenRepository tokens,
        IEmailService email)
    {
        _users = users;
        _tokens = tokens;
        _email = email;
    }

    public async Task Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email, ct);
        if (user is null)
            return; // Silent — no enumeration

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        var token = PasswordResetToken.Create(user.Id, hash, DateTime.UtcNow.AddHours(1));
        await _tokens.AddAsync(token, ct);
        await _tokens.SaveChangesAsync(ct);

        await _email.SendPasswordResetEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", raw, ct);
    }
}
