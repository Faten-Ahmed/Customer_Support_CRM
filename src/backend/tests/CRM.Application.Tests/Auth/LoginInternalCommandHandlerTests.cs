using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using CRM.Domain.Auth;
using CRM.Domain.Customers;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class LoginInternalCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly LoginInternalCommandHandler _handler;

    public LoginInternalCommandHandlerTests()
    {
        _handler = new LoginInternalCommandHandler(
            _userRepo.Object, _customerRepo.Object, _tokenService.Object, _refreshRepo.Object);
    }

    private static User MakeUser(bool isActive = true, bool requiresPasswordChange = false)
        => User.CreateForTest(
            email: "agent@crm.test",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
            role: UserRole.Agent,
            isActive: isActive,
            requiresPasswordChange: requiresPasswordChange);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsLoginResponse()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("access-jwt");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("raw", "hash"));

        var result = await _handler.Handle(
            new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), default);

        Assert.Equal("access-jwt", result.AccessToken);
        Assert.Equal("raw", result.RefreshToken);
        Assert.False(result.RequiresPasswordChange);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("agent@crm.test", "wrong"), default));
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.FindByEmailAsync("ghost@crm.test", default)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("ghost@crm.test", "any"), default));
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser(isActive: false);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), default));
    }

    [Fact]
    public async Task Handle_RequiresPasswordChange_FlagIsTrue()
    {
        var user = MakeUser(requiresPasswordChange: true);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("jwt");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("raw", "hash"));

        var result = await _handler.Handle(
            new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), default);

        Assert.True(result.RequiresPasswordChange);
    }
}
