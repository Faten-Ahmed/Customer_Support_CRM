using CRM.Application.CSAT.Events;
using CRM.Application.CSAT.Jobs;
using CRM.Domain.Tickets.Events;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

public class TicketClosedCsatEventHandlerTests
{
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly TicketClosedCsatEventHandler _handler;

    public TicketClosedCsatEventHandlerTests()
    {
        _handler = new TicketClosedCsatEventHandler(_jobs.Object);
    }

    [Fact]
    public async Task Handle_TicketClosedEvent_EnqueuesSendCsatSurveyJob()
    {
        var evt = new TicketClosedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(
            It.Is<Job>(job => job.Type == typeof(SendCsatSurveyJob)),
            It.IsAny<IState>()), Times.Once);
    }
}
