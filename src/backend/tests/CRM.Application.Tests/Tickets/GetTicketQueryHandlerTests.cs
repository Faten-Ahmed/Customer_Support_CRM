using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _repo = new();
    private readonly GetTicketQueryHandler _handler;

    public GetTicketQueryHandlerTests()
    {
        _handler = new GetTicketQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingTicket_ReturnsTicketDetailDto()
    {
        var id = Guid.NewGuid();
        var ticket = Ticket.Create(
            Guid.NewGuid(), "Cannot login", "لا أستطيع تسجيل الدخول", "Description", "الوصف",
            TicketPriority.High, TicketChannel.Internal, Guid.NewGuid());

        _repo.Setup(r => r.FindByIdDetailedAsync(id, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(new GetTicketQuery(id), default);

        Assert.Equal("Cannot login", result.Subject);
        Assert.Equal("New", result.Status);
        Assert.Equal("High", result.Priority);
    }

    [Fact]
    public async Task Handle_NonExistentTicket_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdDetailedAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetTicketQuery(Guid.NewGuid()), default));
    }
}
