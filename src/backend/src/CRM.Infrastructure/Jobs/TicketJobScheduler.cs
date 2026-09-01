using CRM.Application.Common;
using CRM.Application.Tickets.Jobs;
using Hangfire;

namespace CRM.Infrastructure.Jobs;

public class TicketJobScheduler : ITicketJobScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public TicketJobScheduler(IBackgroundJobClient jobs) => _jobs = jobs;

    public void ScheduleAutoAssign(Guid ticketId)
    {
        _jobs.Enqueue<AutoAssignTicketJob>(j => j.Execute(ticketId, CancellationToken.None));
    }
}
