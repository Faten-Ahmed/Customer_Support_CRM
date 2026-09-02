using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketMessagesQueryHandlerTests
{
    private readonly Mock<ITicketMessageRepository> _repo = new();
    private readonly GetTicketMessagesQueryHandler _handler;

    public GetTicketMessagesQueryHandlerTests()
    {
        _handler = new GetTicketMessagesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AgentCaller_PassesIncludeInternalTrue()
    {
        var ticketId = Guid.NewGuid();
        var projections = new List<TicketMessageProjection>
        {
            new(Guid.NewGuid(), ticketId, "Public reply", false, Guid.NewGuid(), "Ali", null, DateTime.UtcNow),
            new(Guid.NewGuid(), ticketId, "Internal note", true, Guid.NewGuid(), "Hassan", null, DateTime.UtcNow)
        };

        _repo.Setup(r => r.ListByTicketAsync(ticketId, true, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketMessageProjection>(projections, 2, 1, 20));

        var result = await _handler.Handle(
            new GetTicketMessagesQuery(ticketId, 1, 20, IsCallerCustomer: false), default);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_CustomerCaller_PassesIncludeInternalFalse()
    {
        var ticketId = Guid.NewGuid();
        var projections = new List<TicketMessageProjection>
        {
            new(Guid.NewGuid(), ticketId, "Public reply", false, Guid.NewGuid(), "Ali", null, DateTime.UtcNow)
        };

        _repo.Setup(r => r.ListByTicketAsync(ticketId, false, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketMessageProjection>(projections, 1, 1, 20));

        var result = await _handler.Handle(
            new GetTicketMessagesQuery(ticketId, 1, 20, IsCallerCustomer: true), default);

        Assert.Single(result.Items);
        Assert.False(result.Items[0].IsInternal);
    }
}
