using CRM.Application.Customers.Queries;
using CRM.Domain.Customers;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class GetCustomerTicketsQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GetCustomerTicketsQueryHandler _handler;

    public GetCustomerTicketsQueryHandlerTests()
    {
        _handler = new GetCustomerTicketsQueryHandler(
            _customers.Object, _tickets.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsAllCustomerTickets()
    {
        var customerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);

        _customers.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _tickets.Setup(r => r.ListByCustomerAsync(
            customerId, null, null, 1, 20, default))
            .ReturnsAsync(new PagedResult<CustomerTicketProjection>(
                new List<CustomerTicketProjection>
                {
                    new("TKT-001", "Login issue", "Open", "High", DateTime.UtcNow, "Technical")
                }, 1, 1, 20));

        var result = await _handler.Handle(
            new GetCustomerTicketsQuery(customerId, adminId, UserRole.Admin, null, 1, 20),
            default);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TKT-001", result.Items[0].TicketNumber);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _customers.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                  .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new GetCustomerTicketsQuery(Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin, null, 1, 20),
                default));
    }

    [Fact]
    public async Task Handle_AgentScope_ScopesToOwnDepartments()
    {
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _customers.Setup(r => r.FindByIdAsync(customerId, default))
                  .ReturnsAsync(Customer.Create("Bob", "bob@example.com", null, null));
        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });
        _tickets.Setup(r => r.ListByCustomerAsync(
            customerId, null,
            It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)),
            1, 20, default))
            .ReturnsAsync(new PagedResult<CustomerTicketProjection>(
                new List<CustomerTicketProjection>(), 0, 1, 20));

        await _handler.Handle(
            new GetCustomerTicketsQuery(customerId, agentId, UserRole.Agent, null, 1, 20),
            default);

        _users.Verify(u => u.GetDepartmentIdsAsync(agentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_StatusFilter_PassedToRepository()
    {
        var customerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _customers.Setup(r => r.FindByIdAsync(customerId, default))
                  .ReturnsAsync(Customer.Create("Carol", "carol@example.com", null, null));
        _tickets.Setup(r => r.ListByCustomerAsync(
            customerId, "Open", null, 1, 20, default))
            .ReturnsAsync(new PagedResult<CustomerTicketProjection>(
                new List<CustomerTicketProjection>(), 0, 1, 20));

        await _handler.Handle(
            new GetCustomerTicketsQuery(customerId, adminId, UserRole.Admin, "Open", 1, 20),
            default);

        _tickets.Verify(r => r.ListByCustomerAsync(
            customerId, "Open", null, 1, 20, default), Times.Once);
    }
}
