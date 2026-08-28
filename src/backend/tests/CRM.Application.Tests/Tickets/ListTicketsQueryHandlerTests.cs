using CRM.Application.Tickets.Queries;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ListTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly ListTicketsQueryHandler _handler;

    public ListTicketsQueryHandlerTests()
    {
        _handler = new ListTicketsQueryHandler(_repo.Object);
    }

    private static PagedResult<TicketListProjection> EmptyPage() =>
        new(new List<TicketListProjection>(), 0, 1, 20);

    [Fact]
    public async Task Handle_AdminRole_DoesNotForceAssigneeFilter()
    {
        var adminId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(It.IsAny<TicketFilter>(), default))
             .ReturnsAsync(EmptyPage());

        await _handler.Handle(new ListTicketsQuery(
            null, null, null, null, null, 1, 20, "createdAt", false,
            adminId, UserRole.Admin), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<TicketFilter>(f => f.AssignedToUserId == null), default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentRole_ForcesAssigneeFilterToSelf()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(It.IsAny<TicketFilter>(), default))
             .ReturnsAsync(EmptyPage());

        await _handler.Handle(new ListTicketsQuery(
            null, null, null, null, null, 1, 20, "createdAt", false,
            agentId, UserRole.Agent), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<TicketFilter>(f => f.AssignedToUserId == agentId), default), Times.Once);
    }

    [Fact]
    public async Task Handle_StatusFilter_PassedToRepository()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<TicketFilter>(), default))
             .ReturnsAsync(EmptyPage());

        await _handler.Handle(new ListTicketsQuery(
            TicketStatus.New, null, null, null, null, 1, 20, "createdAt", false,
            Guid.NewGuid(), UserRole.Manager), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<TicketFilter>(f => f.Status == TicketStatus.New), default), Times.Once);
    }
}
