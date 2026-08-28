using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal.Profile;

public class PortalProfileTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly GetMyPortalProfileQueryHandler _getHandler;
    private readonly UpdatePortalProfileCommandHandler _updateHandler;

    public PortalProfileTests()
    {
        _getHandler = new GetMyPortalProfileQueryHandler(_repo.Object);
        _updateHandler = new UpdatePortalProfileCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Get_ReturnsCustomerProfile()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", "555-0100", "AcmeCorp");
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _getHandler.Handle(
            new GetMyPortalProfileQuery(customerId), default);

        Assert.Equal("Alice", result.FullName);
        Assert.Equal("alice@example.com", result.Email);
    }

    [Fact]
    public async Task Get_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _getHandler.Handle(new GetMyPortalProfileQuery(customerId), default));
    }

    [Fact]
    public async Task Update_ChangesAllowedFields()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", "555-0100", "AcmeCorp");
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _updateHandler.Handle(
            new UpdatePortalProfileCommand(customerId, "Alicia", "555-9999", "Riyadh"),
            default);

        Assert.Equal("Alicia", result.FullName);
        Assert.Equal("555-9999", result.Phone);
        Assert.Equal("Riyadh", result.City);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("AcmeCorp", result.CompanyName);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Update_NullFields_KeepsExistingValues()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", "555-0100", "AcmeCorp");
        customer.UpdateCity("Dubai");
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _updateHandler.Handle(
            new UpdatePortalProfileCommand(customerId, null, null, null),
            default);

        Assert.Equal("Alice", result.FullName);
        Assert.Equal("Dubai", result.City);
    }
}
