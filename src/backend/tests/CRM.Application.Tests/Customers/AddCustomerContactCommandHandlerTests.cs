using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;

namespace CRM.Application.Tests.Customers;

public class AddCustomerContactCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<ICustomerContactRepository> _contactRepo = new();
    private readonly AddCustomerContactCommandHandler _handler;

    public AddCustomerContactCommandHandlerTests()
    {
        _handler = new AddCustomerContactCommandHandler(_repo.Object, _contactRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidContact_AddsContactAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);
        _contactRepo.Setup(r => r.FindByCustomerIdAsync(id, default)).ReturnsAsync([]);

        var result = await _handler.Handle(
            new AddCustomerContactCommand(id, "Phone", "555-0100", true), default);

        Assert.Equal("Phone", result.Type);
        Assert.Equal("555-0100", result.Value);
        Assert.True(result.IsPrimary);
        _contactRepo.Verify(r => r.AddAsync(It.IsAny<CustomerContact>(), default), Times.Once);
        _contactRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new AddCustomerContactCommand(Guid.NewGuid(), "Phone", "555-0100", true), default));
    }

    [Fact]
    public async Task Handle_InvalidContactType_ThrowsArgumentException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);
        _contactRepo.Setup(r => r.FindByCustomerIdAsync(id, default)).ReturnsAsync([]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new AddCustomerContactCommand(id, "Fax", "555-0100", true), default));
    }

    [Fact]
    public async Task Handle_IsPrimary_DemotesExistingPrimaryOfSameType()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        var existingPrimary = CustomerContact.Create(customerId, "Phone", "555-0100", isPrimary: true);
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _contactRepo.Setup(r => r.FindByCustomerIdAsync(customerId, default))
                    .ReturnsAsync([existingPrimary]);

        await _handler.Handle(new AddCustomerContactCommand(customerId, "Phone", "555-0200", true), default);

        Assert.False(existingPrimary.IsPrimary);
    }
}
