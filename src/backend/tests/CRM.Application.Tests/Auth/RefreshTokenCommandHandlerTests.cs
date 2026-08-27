using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using CRM.Application.Common;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshRepo.Object, _userRepo.Object, _tokenService.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAccessToken()
    {
        const string raw = "valid-raw-token";
        var userId = Guid.NewGuid();
        var stored = RefreshToken.Create(userId, Hash(raw), DateTime.UtcNow.AddDays(7));
        var user = User.CreateForTest(email: "a@b.com", passwordHash: "x",
            role: UserRole.Agent, isActive: true, requiresPasswordChange: false);

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("new-jwt");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("new-raw", "new-hash"));

        var result = await _handler.Handle(new RefreshTokenCommand(raw), default);

        Assert.Equal("new-jwt", result.AccessToken);
        Assert.Equal("new-raw", result.NewRefreshToken);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorizedAccessException()
    {
        _refreshRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                    .ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RefreshTokenCommand("bad"), default));
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsUnauthorizedAccessException()
    {
        const string raw = "revoked-token";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(7));
        stored.Revoke();

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RefreshTokenCommand(raw), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedAccessException()
    {
        const string raw = "expired-token";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(-1));

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RefreshTokenCommand(raw), default));
    }
}
