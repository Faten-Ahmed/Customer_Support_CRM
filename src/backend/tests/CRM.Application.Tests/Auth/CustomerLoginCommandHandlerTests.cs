using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using CRM.Domain.Auth;
using CRM.Domain.Customers;
using CRM.Domain.Users;
using CRM.Application.Common;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class CustomerLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ICustomerCredentialRepository> _credentials = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly LoginInternalCommandHandler _handler;

    public CustomerLoginCommandHandlerTests()
    {
        _handler = new LoginInternalCommandHandler(
            _users.Object, _customers.Object, _credentials.Object,
            _tokenService.Object, _refreshTokens.Object);
    }

    private static Customer MakeCustomer(bool active = true)
    {
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        if (!active) customer.Deactivate();
        return customer;
    }

    private static CustomerCredential MakeCredential(Guid customerId, bool verified = true)
    {
        var cred = CustomerCredential.Create(customerId, BCrypt.Net.BCrypt.HashPassword("CorrectP@ss1!"));
        if (verified) cred.VerifyEmail();
        return cred;
    }

    [Fact]
    public async Task Handle_ValidCustomerCredentials_ReturnsTokenWithCustomerRole()
    {
        var customer = MakeCustomer();
        var credential = MakeCredential(customer.Id);

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);
        _credentials.Setup(r => r.FindByCustomerIdAsync(customer.Id, default)).ReturnsAsync(credential);
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
        var customer = MakeCustomer();
        var credential = MakeCredential(customer.Id, verified: false);

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);
        _credentials.Setup(r => r.FindByCustomerIdAsync(customer.Id, default)).ReturnsAsync(credential);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("alice@example.com", "CorrectP@ss1!"), default));

        Assert.Contains("EMAIL_NOT_VERIFIED", ex.Message);
    }

    [Fact]
    public async Task Handle_AccountInactive_ThrowsUnauthorizedWithCode()
    {
        var customer = MakeCustomer(active: false);
        var credential = MakeCredential(customer.Id, verified: true);

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);
        _credentials.Setup(r => r.FindByCustomerIdAsync(customer.Id, default)).ReturnsAsync(credential);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("alice@example.com", "CorrectP@ss1!"), default));

        Assert.Contains("ACCOUNT_INACTIVE", ex.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var customer = MakeCustomer();
        var credential = MakeCredential(customer.Id);

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default)).ReturnsAsync(customer);
        _credentials.Setup(r => r.FindByCustomerIdAsync(customer.Id, default)).ReturnsAsync(credential);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("alice@example.com", "WrongPassword!"), default));
    }
}
