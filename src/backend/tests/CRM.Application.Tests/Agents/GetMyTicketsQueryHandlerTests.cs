using CRM.Application.Agents.Queries;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class GetMyTicketsQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly GetMyTicketsQueryHandler _handler;

    public GetMyTicketsQueryHandlerTests()
    {
        _handler = new GetMyTicketsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCallerAssignedTickets()
    {
        var agentId = Guid.NewGuid();
        var filter = new AgentTicketFilter(null, null, null, "Priority", "desc");

        _repo.Setup(r => r.ListAssignedToAgentAsync(agentId, filter, 1, 20, default))
             .ReturnsAsync(new PagedResult<MyTicketProjection>(
                 new List<MyTicketProjection>(), 0, 1, 20));

        var result = await _handler.Handle(
            new GetMyTicketsQuery(agentId, null, null, null, 1, 20, "Priority", "desc"),
            default);

        Assert.Equal(0, result.TotalCount);
        _repo.Verify(r => r.ListAssignedToAgentAsync(
            agentId,
            It.Is<AgentTicketFilter>(f => f.SortBy == "Priority"),
            1, 20, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultSort_UsesPriorityDescSlaUrgencyAsc()
    {
        var agentId = Guid.NewGuid();

        _repo.Setup(r => r.ListAssignedToAgentAsync(
            agentId,
            It.Is<AgentTicketFilter>(f => f.SortBy == "Priority" && f.SortDir == "desc"),
            1, 20, default))
             .ReturnsAsync(new PagedResult<MyTicketProjection>(
                 new List<MyTicketProjection>(), 0, 1, 20));

        await _handler.Handle(
            new GetMyTicketsQuery(agentId, null, null, null, 1, 20, null, null),
            default);

        _repo.Verify(r => r.ListAssignedToAgentAsync(
            agentId,
            It.Is<AgentTicketFilter>(f => f.SortBy == "Priority" && f.SortDir == "desc"),
            1, 20, default), Times.Once);
    }
}
