using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;

namespace CRM.Application.Tests.Customers;

public class RemoveCustomerContactCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly RemoveCustomerContactCommandHandler _handler;

    public RemoveCustomerContactCommandHandlerTests()
    {
        _handler = new RemoveCustomerContactCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidRemoval_RemovesContact()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        // Add two contacts so removal doesn't hit sole-primary guard
        customer.AddContact("Phone", "555-0100", isPrimary: true);
        customer.AddContact("Phone", "555-0200", isPrimary: true); // demotes first

        var nonPrimaryId = customer.Contacts.First(c => !c.IsPrimary).Id;
        _repo.Setup(r => r.FindByIdWithContactsAsync(customerId, default)).ReturnsAsync(customer);

        await _handler.Handle(new RemoveCustomerContactCommand(customerId, nonPrimaryId), default);

        Assert.Single(customer.Contacts);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new RemoveCustomerContactCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }
}
