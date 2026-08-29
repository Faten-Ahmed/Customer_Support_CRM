using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AssignTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly AssignTicketCommandHandler _handler;

    public AssignTicketCommandHandlerTests()
    {
        _handler = new AssignTicketCommandHandler(_ticketRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidAssignment_SetsAssigneeAndStatusAssigned()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        var ticket = Ticket.Create(Guid.NewGuid(), "Subj", "موضوع", "Desc", "وصف",
            TicketPriority.Medium, TicketChannel.Internal, managerId);
        var agent = User.CreateForTest("agent@crm.test", "hash",
            UserRole.Agent, true, false, agentId);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(agentId, default)).ReturnsAsync(agent);

        await _handler.Handle(new AssignTicketCommand(ticketId, agentId, managerId), default);

        Assert.Equal(agentId, ticket.AssignedToUserId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_NonExistentAgent_ThrowsKeyNotFoundException()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_InactiveAgent_ThrowsInvalidOperationException()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        var inactiveAgent = User.CreateForTest("agent@crm.test", "hash",
            UserRole.Agent, false, false);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
                   .ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(inactiveAgent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AssignTicketCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "موضوع", "D", "وصف",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        var agent = User.CreateForTest("a@b.com", "hash", UserRole.Agent, true, false);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(agent);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AssignTicketCommand(ticketId, Guid.NewGuid(), Guid.NewGuid()),
                default));
    }
}
