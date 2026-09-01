using CRM.Application.Common;
using CRM.Application.Notifications.Commands;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ReopenTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITicketJobScheduler> _jobs = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly ReopenTicketCommandHandler _handler;

    public ReopenTicketCommandHandlerTests()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Guid.NewGuid());
        _handler = new ReopenTicketCommandHandler(
            _ticketRepo.Object, _userRepo.Object, _jobs.Object, _mediator.Object);
    }

    private static Ticket MakeResolvedTicket(Guid? assignedTo = null)
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "Subject", "موضوع", "Desc", "وصف",
            TicketPriority.Medium, TicketChannel.Portal, Guid.NewGuid());

        if (assignedTo.HasValue)
            ticket.Assign(assignedTo.Value, Guid.NewGuid());
        else
            ticket.ChangeStatus(TicketStatus.Assigned, Guid.NewGuid());

        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Resolved, Guid.NewGuid());
        return ticket;
    }

    [Fact]
    public async Task Handle_ResolvedTicket_TransitionsToReopened()
    {
        var ticket = MakeResolvedTicket();
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default);

        Assert.Equal(TicketStatus.Reopened, ticket.Status);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var ticket = MakeResolvedTicket();
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_AssignedAgentInactive_SchedulesAutoAssign()
    {
        var agentId = Guid.NewGuid();
        var ticket = MakeResolvedTicket(assignedTo: agentId);
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.IsActiveAgentAsync(agentId, default)).ReturnsAsync(false);

        await _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default);

        _jobs.Verify(j => j.ScheduleAutoAssign(ticket.Id), Times.Once);
    }

    [Fact]
    public async Task Handle_AssignedAgentStillActive_DoesNotSchedule()
    {
        var agentId = Guid.NewGuid();
        var ticket = MakeResolvedTicket(assignedTo: agentId);
        _ticketRepo.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.IsActiveAgentAsync(agentId, default)).ReturnsAsync(true);

        await _handler.Handle(new ReopenTicketCommand(ticket.Id, Guid.NewGuid()), default);

        _jobs.Verify(j => j.ScheduleAutoAssign(It.IsAny<Guid>()), Times.Never);
    }
}
