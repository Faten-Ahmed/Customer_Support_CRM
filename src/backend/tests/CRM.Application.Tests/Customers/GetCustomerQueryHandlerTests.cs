using CRM.Application.Customers.Queries;
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class GetCustomerQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly GetCustomerQueryHandler _handler;

    public GetCustomerQueryHandlerTests()
    {
        _handler = new GetCustomerQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingActiveCustomer_ReturnsDetailDto()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali Hassan", "ali@example.com", "0501234567", "Acme");

        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(new GetCustomerQuery(id), default);

        Assert.Equal("Ali Hassan", result.FullName);
        Assert.Equal("ali@example.com", result.Email);
        Assert.Equal("Acme", result.CompanyName);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCustomerQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_SoftDeletedCustomer_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali Hassan", "ali@example.com", null, null);
        customer.Deactivate();

        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCustomerQuery(id), default));
    }
}
