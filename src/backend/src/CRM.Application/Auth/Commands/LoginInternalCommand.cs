using CRM.Application.Auth.DTOs;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record LoginInternalCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginInternalCommandHandler : IRequestHandler<LoginInternalCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IRefreshTokenRepository _refreshTokens;

    public LoginInternalCommandHandler(
        IUserRepository users, ITokenService tokens, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResponse> Handle(LoginInternalCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        var accessToken = _tokens.CreateAccessToken(user);
        var (rawToken, tokenHash) = _tokens.CreateRefreshToken();

        var refreshToken = RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddDays(7));
        await _refreshTokens.AddAsync(refreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: rawToken,
            RequiresPasswordChange: user.RequiresPasswordChange,
            UserId: user.Id,
            FullName: $"{user.FirstName} {user.LastName}",
            Role: user.Role.ToString());
    }
}
