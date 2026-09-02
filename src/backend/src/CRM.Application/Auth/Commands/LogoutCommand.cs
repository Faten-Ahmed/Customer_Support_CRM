using System.Security.Cryptography;
using System.Text;
using CRM.Domain.Auth;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record LogoutCommand(string RawToken) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens)
        => _refreshTokens = refreshTokens;

    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.RawToken)));
        var stored = await _refreshTokens.FindByHashAsync(hash, ct);

        if (stored is null)
            return;

        stored.Revoke();
        await _refreshTokens.SaveChangesAsync(ct);
    }
}
