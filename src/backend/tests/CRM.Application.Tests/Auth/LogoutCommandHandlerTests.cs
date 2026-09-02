using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refreshRepo.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_RevokesToken()
    {
        const string raw = "valid-raw";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(7));

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await _handler.Handle(new LogoutCommand(raw), default);

        Assert.True(stored.IsRevoked);
        _refreshRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_CompletesWithoutError()
    {
        _refreshRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                    .ReturnsAsync((RefreshToken?)null);

        // Should not throw — idempotent logout
        await _handler.Handle(new LogoutCommand("missing"), default);

        _refreshRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_CompletesWithoutError()
    {
        const string raw = "already-revoked";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(7));
        stored.Revoke();

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await _handler.Handle(new LogoutCommand(raw), default);

        // Already revoked — SaveChanges should still be called to persist idempotency
        _refreshRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
