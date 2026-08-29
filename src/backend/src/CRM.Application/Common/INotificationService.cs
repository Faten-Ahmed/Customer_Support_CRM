using CRM.Domain.Sla;

namespace CRM.Application.Common;

public interface INotificationService
{
    Task SendSlaBreachAlertAsync(
        Guid ticketId,
        SlaBreachTier tier,
        Guid? assignedAgentId,
        Guid? departmentId,
        CancellationToken ct = default);
}
