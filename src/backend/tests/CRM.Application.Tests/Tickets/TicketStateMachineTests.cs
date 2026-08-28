using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets.Enums;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class TicketStateMachineTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Assigned, true)]
    [InlineData(TicketStatus.Assigned, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.OnHold, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Escalated, true)]
    [InlineData(TicketStatus.OnHold, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Escalated, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Escalated, TicketStatus.Resolved, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Reopened, true)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed, true)]
    [InlineData(TicketStatus.Reopened, TicketStatus.InProgress, true)]
    [InlineData(TicketStatus.Closed, TicketStatus.Reopened, false)]
    [InlineData(TicketStatus.New, TicketStatus.Resolved, false)]
    [InlineData(TicketStatus.Assigned, TicketStatus.Closed, false)]
    public void IsValidTransition_ReturnsExpectedResult(
        TicketStatus from, TicketStatus to, bool expected)
    {
        Assert.Equal(expected, TicketStateMachine.IsValidTransition(from, to));
    }
}
