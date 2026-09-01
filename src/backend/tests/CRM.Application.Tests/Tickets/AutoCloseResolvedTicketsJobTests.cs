using CRM.Application.Tickets.Jobs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AutoCloseResolvedTicketsJobTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly AutoCloseResolvedTicketsJob _job;

    public AutoCloseResolvedTicketsJobTests()
    {
        _job = new AutoCloseResolvedTicketsJob(_repo.Object);
    }

    [Fact]
    public async Task Execute_ResolvedTicketsOlderThan48h_ClosesThemAll()
    {
        var creatorId = Guid.NewGuid();
        var ticket1 = Ticket.Create(Guid.NewGuid(), "Sub1", "Sub1Ar", "Desc", "DescAr",
            TicketPriority.Low, TicketChannel.Email, creatorId);
        var ticket2 = Ticket.Create(Guid.NewGuid(), "Sub2", "Sub2Ar", "Desc", "DescAr",
            TicketPriority.High, TicketChannel.Portal, creatorId);

        _repo.Setup(r => r.FindResolvedWithNoCustomerReplyAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Ticket> { ticket1, ticket2 });

        await _job.Execute();

        Assert.Equal(TicketStatus.Closed, ticket1.Status);
        Assert.Equal(TicketStatus.Closed, ticket2.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_NoEligibleTickets_DoesNotCallSave()
    {
        _repo.Setup(r => r.FindResolvedWithNoCustomerReplyAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Ticket>());

        await _job.Execute();

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Execute_ClosedTicket_HasAutoClosedHistoryEntry()
    {
        var creatorId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "Sub", "SubAr", "Desc", "DescAr",
            TicketPriority.Medium, TicketChannel.Email, creatorId);

        _repo.Setup(r => r.FindResolvedWithNoCustomerReplyAsync(
            It.IsAny<DateTime>(), default))
            .ReturnsAsync(new List<Ticket> { ticket });

        await _job.Execute();

        Assert.Contains(ticket.History, h => h.FieldChanged == "AutoClosed");
    }
}
