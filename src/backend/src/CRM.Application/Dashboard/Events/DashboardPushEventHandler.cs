using CRM.Application.Dashboard.Services;
using CRM.Domain.Tickets.Events;
using MediatR;

namespace CRM.Application.Dashboard.Events;

public class DashboardPushEventHandler :
    INotificationHandler<TicketCreatedEvent>,
    INotificationHandler<TicketStatusChangedEvent>,
    INotificationHandler<TicketAssignedEvent>,
    INotificationHandler<AgentStatusChangedEvent>
{
    private readonly IDashboardPusher _pusher;

    public DashboardPushEventHandler(IDashboardPusher pusher) => _pusher = pusher;

    public Task Handle(TicketCreatedEvent n, CancellationToken ct)
        => _pusher.ScheduleKpiPushAsync(n.DepartmentId, ct);

    public Task Handle(TicketStatusChangedEvent n, CancellationToken ct)
        => _pusher.ScheduleKpiPushAsync(n.DepartmentId, ct);

    public Task Handle(TicketAssignedEvent n, CancellationToken ct)
        => _pusher.ScheduleWorkloadPushAsync(n.DepartmentId, ct);

    public Task Handle(AgentStatusChangedEvent n, CancellationToken ct)
        => _pusher.ScheduleWorkloadPushAsync(n.DepartmentId, ct);
}
