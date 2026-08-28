using CRM.Application.Tickets.Queries;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketHistoryQueryHandlerTests
{
    private readonly Mock<ITicketHistoryRepository> _repo = new();
    private readonly GetTicketHistoryQueryHandler _handler;

    public GetTicketHistoryQueryHandlerTests()
    {
        _handler = new GetTicketHistoryQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedHistoryEntries()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var projections = new List<TicketHistoryProjection>
        {
            new(Guid.NewGuid(), "Status", "New", "Assigned", agentId, "Ali Hassan", DateTime.UtcNow.AddHours(-2)),
            new(Guid.NewGuid(), "Priority", "Low", "High", agentId, "Ali Hassan", DateTime.UtcNow.AddHours(-1))
        };

        _repo.Setup(r => r.ListByTicketAsync(ticketId, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketHistoryProjection>(projections, 2, 1, 20));

        var result = await _handler.Handle(
            new GetTicketHistoryQuery(ticketId, 1, 20), default);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Status", result.Items[0].FieldChanged);
        Assert.Equal("Ali Hassan", result.Items[0].ChangedByName);
    }

    [Fact]
    public async Task Handle_EmptyHistory_ReturnsEmptyPage()
    {
        var ticketId = Guid.NewGuid();
        _repo.Setup(r => r.ListByTicketAsync(ticketId, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketHistoryProjection>(
                 new List<TicketHistoryProjection>(), 0, 1, 20));

        var result = await _handler.Handle(
            new GetTicketHistoryQuery(ticketId, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
