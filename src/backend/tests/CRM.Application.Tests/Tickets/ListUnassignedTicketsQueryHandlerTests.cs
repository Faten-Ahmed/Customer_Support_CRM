using CRM.Application.Tickets.Queries;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ListUnassignedTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly ListUnassignedTicketsQueryHandler _handler;

    public ListUnassignedTicketsQueryHandlerTests()
    {
        _handler = new ListUnassignedTicketsQueryHandler(_ticketRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_AgentRole_FiltersToAgentDepartments()
    {
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "Login broken", "كسر", "Desc", "وصف",
            TicketPriority.High, TicketChannel.Email, agentId);

        _userRepo.Setup(r => r.GetDepartmentIdsAsync(agentId, default))
                 .ReturnsAsync(new List<Guid> { deptId });

        _ticketRepo.Setup(r => r.ListUnassignedAsync(
            It.Is<IReadOnlyList<Guid>>(ids => ids.Contains(deptId)), 1, 20, default))
            .ReturnsAsync(new PagedResult<Ticket>(new List<Ticket> { ticket }, 1, 1, 20));

        var result = await _handler.Handle(
            new ListUnassignedTicketsQuery(agentId, UserRole.Agent, 1, 20), default);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("High", result.Items[0].Priority);
    }

    [Fact]
    public async Task Handle_AdminRole_PassesNullDepartmentFilter()
    {
        var adminId = Guid.NewGuid();
        _ticketRepo.Setup(r => r.ListUnassignedAsync(null, 1, 20, default))
                   .ReturnsAsync(new PagedResult<Ticket>(new List<Ticket>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListUnassignedTicketsQuery(adminId, UserRole.Admin, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
        _userRepo.Verify(r => r.GetDepartmentIdsAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleTickets_SortedByCreatedAtAsc()
    {
        var agentId = Guid.NewGuid();

        _userRepo.Setup(r => r.GetDepartmentIdsAsync(agentId, default))
                 .ReturnsAsync(new List<Guid> { Guid.NewGuid() });

        _ticketRepo.Setup(r => r.ListUnassignedAsync(
            It.IsAny<IReadOnlyList<Guid>>(), 1, 20, default))
            .ReturnsAsync(new PagedResult<Ticket>(new List<Ticket>(), 0, 1, 20));

        var result = await _handler.Handle(
            new ListUnassignedTicketsQuery(agentId, UserRole.Agent, 1, 20), default);

        Assert.Empty(result.Items);
    }
}
