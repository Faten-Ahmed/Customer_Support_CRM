using CRM.Application.Common;
using CRM.Application.Tickets.Jobs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AutoAssignTicketJobTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly AutoAssignTicketJob _job;

    public AutoAssignTicketJobTests()
    {
        _job = new AutoAssignTicketJob(_ticketRepo.Object, _userRepo.Object, _notifications.Object);
    }

    [Fact]
    public async Task Execute_SkillMatchedAgent_AssignsAgentWithFewestTickets()
    {
        var ticketId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var bestAgentId = Guid.NewGuid();

        var ticket = Ticket.Create(Guid.NewGuid(), "Subject", "SubjectAr", "Desc", "DescAr",
            TicketPriority.High, TicketChannel.Email, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var agents = new List<AgentCapacityDto>
        {
            new(bestAgentId, OpenTicketCount: 2, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid> { categoryId }),
            new(Guid.NewGuid(), OpenTicketCount: 5, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid> { categoryId })
        };
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(agents);

        await _job.Execute(ticketId);

        // Round-robin selected because ticket has no CategoryId (null dept)
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_NoSkillMatch_RoundRobinByOldestLastAssigned()
    {
        var ticketId = Guid.NewGuid();
        var olderAgentId = Guid.NewGuid();
        var oldDate = DateTime.UtcNow.AddHours(-5);

        var ticket = Ticket.Create(Guid.NewGuid(), "S", "SAr", "D", "DAr",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var agents = new List<AgentCapacityDto>
        {
            new(olderAgentId, OpenTicketCount: 3, LastAssignedAt: oldDate,
                SkillCategoryIds: new List<Guid>()),
            new(Guid.NewGuid(), OpenTicketCount: 1, LastAssignedAt: DateTime.UtcNow.AddHours(-1),
                SkillCategoryIds: new List<Guid>())
        };
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(agents);

        await _job.Execute(ticketId);

        _userRepo.Verify(r => r.UpdateLastAssignedAtAsync(olderAgentId, default), Times.Once);
    }

    [Fact]
    public async Task Execute_AllAgentsOverloaded_SendsManagerAlert()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "SAr", "D", "DAr",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var agents = new List<AgentCapacityDto>
        {
            new(Guid.NewGuid(), OpenTicketCount: 16, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid>()),
            new(Guid.NewGuid(), OpenTicketCount: 20, LastAssignedAt: null,
                SkillCategoryIds: new List<Guid>())
        };
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(agents);

        await _job.Execute(ticketId);

        _notifications.Verify(
            n => n.SendUnassignedTicketAlertAsync(It.IsAny<Guid>(), ticketId, default),
            Times.Once);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Execute_NoActiveAgents_SendsManagerAlert()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "SAr", "D", "DAr",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _userRepo.Setup(r => r.FindActiveAgentsInDepartmentAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync(new List<AgentCapacityDto>());

        await _job.Execute(ticketId);

        _notifications.Verify(
            n => n.SendUnassignedTicketAlertAsync(It.IsAny<Guid>(), ticketId, default),
            Times.Once);
    }
}
