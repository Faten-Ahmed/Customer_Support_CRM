using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(
            _tokenRepo.Object, _userRepo.Object, _refreshRepo.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordAndMarksTokenUsed()
    {
        const string raw = "valid-reset-token";
        var userId = Guid.NewGuid();
        var prt = PasswordResetToken.Create(userId, Hash(raw), DateTime.UtcNow.AddHours(1));
        var user = User.CreateForTest("a@b.com", "oldhash", UserRole.Agent, true, false);

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default);

        Assert.True(prt.IsUsed);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewP@ss1!", user.PasswordHash));
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsInvalidOperationException()
    {
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((PasswordResetToken?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ResetPasswordCommand("bad", "NewP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsInvalidOperationException()
    {
        const string raw = "expired";
        var prt = PasswordResetToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddHours(-1));

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_UsedToken_ThrowsInvalidOperationException()
    {
        const string raw = "used-token";
        var prt = PasswordResetToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddHours(1));
        prt.MarkUsed();

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesAllRefreshTokensForUser()
    {
        const string raw = "valid-for-revoke";
        var userId = Guid.NewGuid();
        var prt = PasswordResetToken.Create(userId, Hash(raw), DateTime.UtcNow.AddHours(1));
        var user = User.CreateForTest("a@b.com", "oldhash", UserRole.Agent, true, false);

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default);

        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(userId, default), Times.Once);
    }
}
