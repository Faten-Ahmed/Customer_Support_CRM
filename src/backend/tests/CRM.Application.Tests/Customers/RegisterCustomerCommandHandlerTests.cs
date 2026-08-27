using CRM.Application.Common;
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class RegisterCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ICustomerCredentialRepository> _credentials = new();
    private readonly Mock<IEmailVerificationTokenRepository> _tokens = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly RegisterCustomerCommandHandler _handler;

    public RegisterCustomerCommandHandlerTests()
    {
        _handler = new RegisterCustomerCommandHandler(
            _customers.Object, _credentials.Object, _tokens.Object, _email.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesCustomerCredentialAndSendsEmail()
    {
        _customers.Setup(r => r.FindByEmailAsync("jane@example.com", default))
                  .ReturnsAsync((Customer?)null);

        var cmd = new RegisterCustomerCommand(
            "Jane Doe", "jane@example.com", "SecurePass1!");

        await _handler.Handle(cmd, default);

        _customers.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Once);
        _credentials.Verify(r => r.AddAsync(It.IsAny<CustomerCredential>(), default), Times.Once);
        _tokens.Verify(r => r.AddAsync(It.IsAny<EmailVerificationToken>(), default), Times.Once);
        _email.Verify(e => e.SendVerificationEmailAsync(
            "jane@example.com", "Jane Doe", It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var existing = Customer.Create("Bob", "jane@example.com", null, null);
        _customers.Setup(r => r.FindByEmailAsync("jane@example.com", default))
                  .ReturnsAsync(existing);

        var cmd = new RegisterCustomerCommand("Jane", "jane@example.com", "SecurePass1!");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(cmd, default));

        _customers.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Never);
    }
}
