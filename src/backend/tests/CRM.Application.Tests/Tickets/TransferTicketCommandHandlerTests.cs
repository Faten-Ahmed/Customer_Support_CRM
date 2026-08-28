using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class TransferTicketCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly TransferTicketCommandHandler _handler;

    public TransferTicketCommandHandlerTests()
    {
        _handler = new TransferTicketCommandHandler(_ticketRepo.Object, _userRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidTransfer_UpdatesDepartmentAndAgent()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());

        var newAgentId = Guid.NewGuid();
        var newDeptId = Guid.NewGuid();
        var agent = User.CreateForTest("a@b.com", "h", UserRole.Agent, true, false, newAgentId);

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindByIdAsync(newAgentId, default)).ReturnsAsync(agent);

        await _handler.Handle(new TransferTicketCommand(
            ticketId, newDeptId, newAgentId, "Specialist needed", Guid.NewGuid()), default);

        Assert.Equal(newDeptId, ticket.DepartmentId);
        Assert.Equal(newAgentId, ticket.AssignedToUserId);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new TransferTicketCommand(
                id, Guid.NewGuid(), null, "reason", Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_DeptTransferOnlyNoAgent_ClearsAssignee()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.Assign(Guid.NewGuid(), Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        await _handler.Handle(new TransferTicketCommand(
            id, Guid.NewGuid(), null, "Dept only transfer", Guid.NewGuid()), default);

        Assert.Null(ticket.AssignedToUserId);
        Assert.Equal(TicketStatus.New, ticket.Status);
    }
}
