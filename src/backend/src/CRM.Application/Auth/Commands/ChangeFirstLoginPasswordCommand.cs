using CRM.Domain.Auth;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record ChangeFirstLoginPasswordCommand(
    string Email,
    string CurrentPassword,
    string NewPassword) : IRequest;

public class ChangeFirstLoginPasswordCommandHandler
    : IRequestHandler<ChangeFirstLoginPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public ChangeFirstLoginPasswordCommandHandler(
        IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(ChangeFirstLoginPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        if (BCrypt.Net.BCrypt.Verify(cmd.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("New password must differ from the current password.");

        user.SetPassword(BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword));

        await _refreshTokens.RevokeAllForUserAsync(user.Id, ct);
        await _users.SaveChangesAsync(ct);
    }
}
