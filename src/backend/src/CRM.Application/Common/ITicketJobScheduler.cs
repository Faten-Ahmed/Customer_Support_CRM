namespace CRM.Application.Common;

public interface ITicketJobScheduler
{
    void ScheduleAutoAssign(Guid ticketId);
}
