using CRM.Application.Common;
using CRM.Domain.Sla;

namespace CRM.Infrastructure.Notifications;

// TODO: Replace with real notification dispatch (SignalR + email) in Feature 05
public class StubNotificationService : INotificationService
{
    public Task SendSlaBreachAlertAsync(
        Guid ticketId,
        SlaBreachTier tier,
        Guid? assignedAgentId,
        Guid? departmentId,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
