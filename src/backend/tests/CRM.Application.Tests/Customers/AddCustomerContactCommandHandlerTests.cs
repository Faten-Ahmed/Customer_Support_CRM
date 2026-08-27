using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;

namespace CRM.Application.Tests.Customers;

public class AddCustomerContactCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly AddCustomerContactCommandHandler _handler;

    public AddCustomerContactCommandHandlerTests()
    {
        _handler = new AddCustomerContactCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidContact_AddsContactAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(
            new AddCustomerContactCommand(id, "Phone", "555-0100", true), default);

        Assert.Single(customer.Contacts);
        Assert.Equal("Phone", customer.Contacts.First().Type);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new AddCustomerContactCommand(Guid.NewGuid(), "Phone", "555-0100", true), default));
    }

    [Fact]
    public async Task Handle_InvalidContactType_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new AddCustomerContactCommand(id, "Fax", "555-0100", true), default));
    }
}
