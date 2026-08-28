using CRM.Application.Portal.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal.Tickets;

public class ClosePortalTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly ClosePortalTicketCommandHandler _handler;

    public ClosePortalTicketCommandHandlerTests()
    {
        _handler = new ClosePortalTicketCommandHandler(_tickets.Object);
    }

    private static Ticket MakeTicket(Guid customerId) =>
        Ticket.Create(customerId, "Test", "Desc",
            TicketPriority.Medium, TicketChannel.Portal, customerId);

    [Fact]
    public async Task Handle_OpenTicket_ClosesAndSaves()
    {
        var customerId = Guid.NewGuid();
        var ticket = MakeTicket(customerId);
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(
            new ClosePortalTicketCommand(ticket.Id, customerId), default);

        Assert.Equal("Closed", result.Status);
        _tickets.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyClosed_ThrowsInvalidOperationException()
    {
        var customerId = Guid.NewGuid();
        var ticket = MakeTicket(customerId);
        ticket.CloseByCustomer();
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ClosePortalTicketCommand(ticket.Id, customerId), default));

        Assert.Contains("TICKET_ALREADY_CLOSED", ex.Message);
    }

    [Fact]
    public async Task Handle_OtherCustomerTicket_ThrowsUnauthorizedAccessException()
    {
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var ticket = MakeTicket(otherCustomerId);
        _tickets.Setup(r => r.FindByIdAsync(ticket.Id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ClosePortalTicketCommand(ticket.Id, customerId), default));
    }

    [Fact]
    public async Task Handle_TicketNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _tickets.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new ClosePortalTicketCommand(id, Guid.NewGuid()), default));
    }
}
