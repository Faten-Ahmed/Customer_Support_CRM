using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class TransferTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly TransferTicketCommandHandler _handler;

    public TransferTicketCommandHandlerTests()
    {
        _handler = new TransferTicketCommandHandler(_ticketRepo.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_ValidTransfer_UpdatesDepartmentAndClearsAssignee()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Medium, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());

        var newDeptId = Guid.NewGuid();

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.IsDepartmentActiveAsync(newDeptId, default)).ReturnsAsync(true);

        await _handler.Handle(new TransferTicketCommand(
            ticketId, newDeptId, "Specialist needed", Guid.NewGuid()), default);

        Assert.Equal(newDeptId, ticket.DepartmentId);
        Assert.Null(ticket.AssignedToUserId);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new TransferTicketCommand(
                id, Guid.NewGuid(), "some reason", Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_InactiveDepartment_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());

        var badDeptId = Guid.NewGuid();
        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);
        _ticketRepo.Setup(r => r.IsDepartmentActiveAsync(badDeptId, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new TransferTicketCommand(
                id, badDeptId, "Dept only transfer", Guid.NewGuid()), default));
    }
}
