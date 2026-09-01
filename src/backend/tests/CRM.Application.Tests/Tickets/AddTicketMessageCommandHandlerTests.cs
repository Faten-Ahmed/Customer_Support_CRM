using CRM.Application.Notifications.Commands;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AddTicketMessageCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ITicketMessageRepository> _messageRepo = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly AddTicketMessageCommandHandler _handler;

    public AddTicketMessageCommandHandlerTests()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Guid.NewGuid());
        _handler = new AddTicketMessageCommandHandler(
            _ticketRepo.Object, _messageRepo.Object,
            _users.Object, _customers.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_ValidMessage_AddsMessageAndReturnsDto()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Medium, TicketChannel.Internal, agentId);
        ticket.Assign(agentId, agentId);
        ticket.ChangeStatus(TicketStatus.InProgress, agentId);

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(new AddTicketMessageCommand(
            ticketId, "<p>Hello customer</p>", false, agentId, null), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("<p>Hello customer</p>", result.Body);
        Assert.False(result.IsInternal);
        _messageRepo.Verify(r => r.AddAsync(It.IsAny<TicketMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_InternalNote_IsMarkedInternal()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        TicketMessage? captured = null;
        _messageRepo.Setup(r => r.AddAsync(It.IsAny<TicketMessage>(), default))
                    .Callback<TicketMessage, CancellationToken>((m, _) => captured = m)
                    .Returns(Task.CompletedTask);

        await _handler.Handle(new AddTicketMessageCommand(
            ticketId, "Internal only", true, Guid.NewGuid(), null), default);

        Assert.NotNull(captured);
        Assert.True(captured!.IsInternal);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new AddTicketMessageCommand(
                ticketId, "msg", false, Guid.NewGuid(), null), default));
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _ticketRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new AddTicketMessageCommand(
                Guid.NewGuid(), "msg", false, Guid.NewGuid(), null), default));
    }
}
