using CRM.Application.Common;
using CRM.Application.Notifications.Commands;
using CRM.Domain.Notifications;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Infrastructure.Notifications;

public class SlaNotificationService : INotificationService
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IMediator _mediator;

    public SlaNotificationService(
        ITicketRepository tickets,
        IUserRepository users,
        IMediator mediator)
    {
        _tickets = tickets;
        _users = users;
        _mediator = mediator;
    }

    public async Task SendSlaBreachAlertAsync(
        Guid ticketId,
        SlaBreachTier tier,
        Guid? assignedAgentId,
        Guid? departmentId,
        CancellationToken ct = default)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId, ct);
        if (ticket is null) return;

        var (notifType, title, body) = tier switch
        {
            SlaBreachTier.Warning =>
                (NotificationType.SlaWarning,
                 $"SLA Warning: Ticket #{ticket.TicketNumber}",
                 $"Ticket #{ticket.TicketNumber} \"{ticket.Subject}\" is approaching its SLA deadline."),
            SlaBreachTier.Breach =>
                (NotificationType.SlaBreached,
                 $"SLA Breached: Ticket #{ticket.TicketNumber}",
                 $"Ticket #{ticket.TicketNumber} \"{ticket.Subject}\" has breached its SLA."),
            SlaBreachTier.CriticalBreach =>
                (NotificationType.SlaCriticalBreach,
                 $"SLA Critical Breach: Ticket #{ticket.TicketNumber}",
                 $"Ticket #{ticket.TicketNumber} \"{ticket.Subject}\" has critically breached its SLA."),
            _ => default
        };

        if (notifType == default) return;

        if (assignedAgentId.HasValue)
        {
            await _mediator.Send(new CreateNotificationCommand(
                assignedAgentId.Value, notifType, title, body, "ticket", ticketId), ct);
        }

        if (tier >= SlaBreachTier.Breach)
        {
            var managers = await _users.ListAsync(UserRole.Manager, null, true, null, 1, 200, ct);
            var admins = await _users.ListAsync(UserRole.Admin, null, true, null, 1, 200, ct);

            foreach (var userId in managers.Items.Concat(admins.Items).Select(u => u.Id).Distinct())
            {
                if (userId == assignedAgentId) continue;
                await _mediator.Send(new CreateNotificationCommand(
                    userId, notifType, title, body, "ticket", ticketId), ct);
            }
        }
    }

    public async Task SendUnassignedTicketAlertAsync(
        Guid departmentId,
        Guid ticketId,
        CancellationToken ct = default)
    {
        var title = "Unassigned Ticket Alert";
        var body = $"Ticket {ticketId} could not be auto-assigned in department {departmentId}. Manual assignment required.";

        var managers = await _users.ListAsync(UserRole.Manager, null, true, null, 1, 200, ct);
        var admins = await _users.ListAsync(UserRole.Admin, null, true, null, 1, 200, ct);

        foreach (var userId in managers.Items.Concat(admins.Items).Select(u => u.Id).Distinct())
        {
            await _mediator.Send(new CreateNotificationCommand(
                userId, NotificationType.UnassignedTicketAlert, title, body, "ticket", ticketId), ct);
        }
    }
}
