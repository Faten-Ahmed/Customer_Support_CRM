using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;

namespace CRM.Application.Tests.Customers;

public class RemoveCustomerContactCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<ICustomerContactRepository> _contactRepo = new();
    private readonly RemoveCustomerContactCommandHandler _handler;

    public RemoveCustomerContactCommandHandlerTests()
    {
        _handler = new RemoveCustomerContactCommandHandler(_repo.Object, _contactRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidRemoval_RemovesContact()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        var contact = CustomerContact.Create(customerId, "Phone", "555-0100", isPrimary: false);

        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _contactRepo.Setup(r => r.FindByIdAsync(contact.Id, default)).ReturnsAsync(contact);

        await _handler.Handle(new RemoveCustomerContactCommand(customerId, contact.Id), default);

        _contactRepo.Verify(r => r.Remove(contact), Times.Once);
        _contactRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new RemoveCustomerContactCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ContactNotFound_ThrowsKeyNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _contactRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                    .ReturnsAsync((CustomerContact?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new RemoveCustomerContactCommand(customerId, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ContactBelongsToDifferentCustomer_ThrowsInvalidOperationException()
    {
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        var contact = CustomerContact.Create(otherCustomerId, "Phone", "555-0100", isPrimary: false);

        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _contactRepo.Setup(r => r.FindByIdAsync(contact.Id, default)).ReturnsAsync(contact);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new RemoveCustomerContactCommand(customerId, contact.Id), default));
    }
}
