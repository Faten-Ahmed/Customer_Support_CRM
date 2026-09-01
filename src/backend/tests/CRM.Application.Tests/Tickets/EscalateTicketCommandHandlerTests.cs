using CRM.Application.Notifications.Commands;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class EscalateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly EscalateTicketCommandHandler _handler;

    public EscalateTicketCommandHandlerTests()
    {
        _userRepo.Setup(r => r.ListAsync(It.IsAny<UserRole?>(), null, true, null, 1, 200, default))
                 .ReturnsAsync(new PagedResult<UserSummaryProjection>([], 0, 1, 200));
        _mediator.Setup(m => m.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Guid.NewGuid());
        _handler = new EscalateTicketCommandHandler(_repo.Object, _userRepo.Object, _mediator.Object);
    }

    private static Ticket MakeInProgressTicket()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.High, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.InProgress, Guid.NewGuid());
        return ticket;
    }

    [Fact]
    public async Task Handle_InProgressTicket_EscalatesAndRecordsReason()
    {
        var id = Guid.NewGuid();
        var ticket = MakeInProgressTicket();
        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await _handler.Handle(
            new EscalateTicketCommand(id, "SLA about to breach", Guid.NewGuid()), default);

        Assert.Equal(TicketStatus.Escalated, ticket.Status);
        Assert.Contains(ticket.History, h => h.FieldChanged == "EscalationReason");
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NewTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new EscalateTicketCommand(id, "reason", Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new EscalateTicketCommand(Guid.NewGuid(), "r", Guid.NewGuid()), default));
    }
}
