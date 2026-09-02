using CRM.Application.Customers.Commands;
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _handler = new CreateCustomerCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCustomerAndReturnsDto()
    {
        _repo.Setup(r => r.FindByEmailAsync("alice@example.com", default))
             .ReturnsAsync((Customer?)null);

        var cmd = new CreateCustomerCommand(
            "Alice Hassan", "أليس حسن", "alice@example.com", "0501234567", "Acme", null);

        var result = await _handler.Handle(cmd, default);

        Assert.Equal("Alice Hassan", result.FullName);
        Assert.Equal("alice@example.com", result.Email);
        _repo.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var existing = Customer.Create("Bob", "بوب", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByEmailAsync("alice@example.com", default))
             .ReturnsAsync(existing);

        var cmd = new CreateCustomerCommand(
            "Alice", "أليس", "alice@example.com", null, null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(cmd, default));

        _repo.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Never);
    }
}
