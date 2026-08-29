using System.Security.Cryptography;
using System.Text;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokens,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens)
    {
        _tokens = tokens;
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.Token)));
        var prt = await _tokens.FindByHashAsync(hash, ct);

        if (prt is null || !prt.IsValid)
            throw new InvalidOperationException("Invalid or expired password reset token.");

        var user = await _users.FindByIdAsync(prt.UserId, ct)
            ?? throw new InvalidOperationException("User not found.");

        user.SetPassword(BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword));
        prt.MarkUsed();

        await _refreshTokens.RevokeAllForUserAsync(prt.UserId, ct);
        await _users.SaveChangesAsync(ct);
    }
}
