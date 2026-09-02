using CRM.Application.CSAT.Jobs;
using CRM.Domain.Tickets.Events;
using Hangfire;
using MediatR;

namespace CRM.Application.CSAT.Events;

public class TicketClosedCsatEventHandler : INotificationHandler<TicketClosedEvent>
{
    private readonly IBackgroundJobClient _jobs;
    public TicketClosedCsatEventHandler(IBackgroundJobClient jobs) => _jobs = jobs;

    public Task Handle(TicketClosedEvent notification, CancellationToken ct)
    {
        _jobs.Enqueue<SendCsatSurveyJob>(
            j => j.ExecuteAsync(
                notification.TicketId,
                notification.AgentId,
                notification.DepartmentId));
        return Task.CompletedTask;
    }
}
