using CRM.Application.Customers.Commands;
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using Moq;

namespace CRM.Application.Tests.Customers;

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly UpdateCustomerCommandHandler _handler;

    public UpdateCustomerCommandHandlerTests()
    {
        _handler = new UpdateCustomerCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_UpdatesAndReturnsDto()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(
            new UpdateCustomerCommand(id, "Alice Updated", "أليس محدثة", "0501234567", "Acme", null), default);

        Assert.Equal("Alice Updated", result.FullName);
        Assert.Equal("0501234567", result.Phone);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new UpdateCustomerCommand(Guid.NewGuid(), "Name", "اسم", null, null, null), default));
    }
}
