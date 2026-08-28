using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class UpdateTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly UpdateTicketCommandHandler _handler;

    public UpdateTicketCommandHandlerTests()
    {
        _handler = new UpdateTicketCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ChangesSubjectAndPriority()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(
            Guid.NewGuid(), "Old Subject", "Old Desc",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(new UpdateTicketCommand(
            id, "New Subject", "New Desc", TicketPriority.High,
            null, null, null, Guid.NewGuid()), default);

        Assert.Equal("New Subject", result.Subject);
        Assert.Equal("High", result.Priority);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new UpdateTicketCommand(
                Guid.NewGuid(), "S", "D", TicketPriority.Low,
                null, null, null, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(
            Guid.NewGuid(), "Subj", "Desc",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UpdateTicketCommand(
                id, "S", "D", TicketPriority.Low,
                null, null, null, Guid.NewGuid()), default));
    }
}
