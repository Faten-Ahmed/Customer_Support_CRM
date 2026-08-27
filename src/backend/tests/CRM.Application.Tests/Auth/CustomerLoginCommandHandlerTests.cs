using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using CRM.Domain.Auth;
using CRM.Domain.Customers;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class CustomerLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly LoginInternalCommandHandler _handler;

    public CustomerLoginCommandHandlerTests()
    {
        _handler = new LoginInternalCommandHandler(
            _users.Object, _customers.Object, _tokenService.Object, _refreshTokens.Object);
    }

    private static Customer MakeCustomer(bool verified = true, bool active = true)
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.SetPassword(BCrypt.Net.BCrypt.HashPassword("CorrectP@ss1!"));
        if (verified) customer.VerifyEmail();
        if (!active) customer.Deactivate();
        return customer;
    }

    [Fact]
    public async Task Handle_ValidCustomerCredentials_ReturnsTokenWithCustomerRole()
    {
        var customer = MakeCustomer();
        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);
        _tokenService.Setup(t => t.CreateAccessToken(customer.Id, "alice@example.com", "Customer", "Alice"))
                     .Returns("customer-jwt-token");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("raw-refresh", "hash"));

        var result = await _handler.Handle(
            new LoginInternalCommand("alice@example.com", "CorrectP@ss1!"), default);

        Assert.Equal("customer-jwt-token", result.AccessToken);
        Assert.Equal("Customer", result.Role);
        Assert.False(result.RequiresPasswordChange);
    }

    [Fact]
    public async Task Handle_EmailNotVerified_ThrowsUnauthorizedWithCode()
    {
        var customer = MakeCustomer(verified: false);
        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("alice@example.com", "CorrectP@ss1!"), default));

        Assert.Contains("EMAIL_NOT_VERIFIED", ex.Message);
    }

    [Fact]
    public async Task Handle_AccountInactive_ThrowsUnauthorizedWithCode()
    {
        var customer = MakeCustomer(verified: true, active: false);
        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("alice@example.com", "CorrectP@ss1!"), default));

        Assert.Contains("ACCOUNT_INACTIVE", ex.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var customer = MakeCustomer();
        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("alice@example.com", "WrongPassword!"), default));
    }
}
