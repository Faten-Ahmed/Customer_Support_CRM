using CRM.Application.Common;

namespace CRM.Infrastructure.Jobs;

// Stub — real Hangfire implementation follows in BE infrastructure tasks.
public class TicketJobScheduler : ITicketJobScheduler
{
    public void ScheduleAutoAssign(Guid ticketId) { }
}
