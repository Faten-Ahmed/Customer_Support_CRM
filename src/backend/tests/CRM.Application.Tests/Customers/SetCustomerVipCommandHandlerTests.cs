using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;

namespace CRM.Application.Tests.Customers;

public class SetCustomerVipCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly SetCustomerVipCommandHandler _handler;

    public SetCustomerVipCommandHandlerTests()
    {
        _handler = new SetCustomerVipCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_SetsVipStatus()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Alice", "أليس", "alice@example.com", null, null);
        _repo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);

        await _handler.Handle(new SetCustomerVipCommand(id, true), default);

        Assert.True(customer.IsVip);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new SetCustomerVipCommand(Guid.NewGuid(), true), default));
    }
}
