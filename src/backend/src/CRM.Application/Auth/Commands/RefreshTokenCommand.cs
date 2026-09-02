using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.DTOs;
using CRM.Application.Common;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record RefreshTokenCommand(string RawToken) : IRequest<RefreshTokenResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens, IUserRepository users, ITokenService tokens)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _tokens = tokens;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.RawToken)));
        var stored = await _refreshTokens.FindByHashAsync(hash, ct);

        if (stored is null || !stored.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await _users.FindByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        stored.Revoke();

        var accessToken = _tokens.CreateAccessToken(user);
        var (newRaw, newHash) = _tokens.CreateRefreshToken();

        var newToken = RefreshToken.Create(user.Id, newHash, DateTime.UtcNow.AddDays(7));
        await _refreshTokens.AddAsync(newToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new RefreshTokenResponse(accessToken, newRaw);
    }
}
