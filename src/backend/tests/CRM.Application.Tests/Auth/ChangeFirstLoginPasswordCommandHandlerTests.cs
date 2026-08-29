using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class ChangeFirstLoginPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly ChangeFirstLoginPasswordCommandHandler _handler;

    public ChangeFirstLoginPasswordCommandHandlerTests()
    {
        _handler = new ChangeFirstLoginPasswordCommandHandler(
            _userRepo.Object, _refreshRepo.Object);
    }

    private static User MakeUser(bool requiresChange = true)
        => User.CreateForTest(
            email: "newagent@crm.test",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("OldP@ss1!"),
            role: UserRole.Agent,
            isActive: true,
            requiresPasswordChange: requiresChange);

    [Fact]
    public async Task Handle_ValidCurrentPassword_ChangesPasswordAndClearsFlag()
    {
        var user = MakeUser(requiresChange: true);
        _userRepo.Setup(r => r.FindByEmailAsync("newagent@crm.test", default)).ReturnsAsync(user);

        await _handler.Handle(
            new ChangeFirstLoginPasswordCommand("newagent@crm.test", "OldP@ss1!", "NewP@ss2!"), default);

        Assert.False(user.RequiresPasswordChange);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewP@ss2!", user.PasswordHash));
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("newagent@crm.test", default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ChangeFirstLoginPasswordCommand("newagent@crm.test", "WrongP@ss!", "NewP@ss2!"), default));
    }

    [Fact]
    public async Task Handle_SamePassword_ThrowsInvalidOperationException()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("newagent@crm.test", default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ChangeFirstLoginPasswordCommand("newagent@crm.test", "OldP@ss1!", "OldP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_ValidPassword_RevokesAllRefreshTokens()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("newagent@crm.test", default)).ReturnsAsync(user);

        await _handler.Handle(
            new ChangeFirstLoginPasswordCommand("newagent@crm.test", "OldP@ss1!", "NewP@ss2!"), default);

        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(user.Id, default), Times.Once);
    }
}
