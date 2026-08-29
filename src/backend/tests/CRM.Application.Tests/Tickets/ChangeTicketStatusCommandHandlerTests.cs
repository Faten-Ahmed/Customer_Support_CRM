using CRM.Application.Tickets.Commands;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class ChangeTicketStatusCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly Mock<ITicketSlaRepository> _slaRepo = new();
    private readonly ChangeTicketStatusCommandHandler _handler;

    public ChangeTicketStatusCommandHandlerTests()
    {
        _slaRepo.Setup(r => r.FindByTicketIdAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync((TicketSla?)null);
        _handler = new ChangeTicketStatusCommandHandler(_repo.Object, _slaRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidTransition_ChangesStatus()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Medium, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid()); // → Assigned

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await _handler.Handle(
            new ChangeTicketStatusCommand(id, TicketStatus.InProgress, Guid.NewGuid()), default);

        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidTransition_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ChangeTicketStatusCommand(id, TicketStatus.Resolved, Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new ChangeTicketStatusCommand(Guid.NewGuid(), TicketStatus.InProgress, Guid.NewGuid()),
                default));
    }
}
