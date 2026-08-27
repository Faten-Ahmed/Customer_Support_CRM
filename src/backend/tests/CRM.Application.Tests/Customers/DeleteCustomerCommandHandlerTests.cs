using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using Moq;

namespace CRM.Application.Tests.Customers;

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly DeleteCustomerCommandHandler _handler;

    public DeleteCustomerCommandHandlerTests()
    {
        _handler = new DeleteCustomerCommandHandler(_customers.Object, _tickets.Object);
    }

    [Fact]
    public async Task Handle_NoOpenTickets_DeactivatesCustomer()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        _customers.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);
        _tickets.Setup(r => r.HasOpenTicketsAsync(id, default)).ReturnsAsync(false);

        await _handler.Handle(new DeleteCustomerCommand(id), default);

        Assert.False(customer.IsActive);
        _customers.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_HasOpenTickets_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        _customers.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);
        _tickets.Setup(r => r.HasOpenTicketsAsync(id, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new DeleteCustomerCommand(id), default));
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _customers.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                  .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new DeleteCustomerCommand(Guid.NewGuid()), default));
    }
}
