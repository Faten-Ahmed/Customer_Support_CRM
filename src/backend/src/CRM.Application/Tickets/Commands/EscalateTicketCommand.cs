using CRM.Application.Notifications.Commands;
using CRM.Application.Tickets.Services;
using CRM.Domain.Notifications;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record EscalateTicketCommand(
    Guid TicketId,
    string Reason,
    Guid EscalatedByUserId) : IRequest;

public class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;
    private readonly IMediator _mediator;

    public EscalateTicketCommandHandler(
        ITicketRepository tickets,
        IUserRepository users,
        IMediator mediator)
    {
        _tickets = tickets;
        _users = users;
        _mediator = mediator;
    }

    public async Task Handle(EscalateTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (!TicketStateMachine.IsValidTransition(ticket.Status, TicketStatus.Escalated))
            throw new InvalidOperationException(
                $"Cannot escalate a ticket in {ticket.Status} status.");

        ticket.ChangeStatus(TicketStatus.Escalated, cmd.EscalatedByUserId);
        ticket.RecordEscalationReason(cmd.Reason, cmd.EscalatedByUserId);
        await _tickets.SaveChangesAsync(ct);

        var title = $"Ticket Escalated: #{ticket.TicketNumber}";
        var body = $"Ticket #{ticket.TicketNumber} \"{ticket.Subject}\" has been escalated. Reason: {cmd.Reason}";

        var managers = await _users.ListAsync(UserRole.Manager, null, true, null, 1, 200, ct);
        var admins = await _users.ListAsync(UserRole.Admin, null, true, null, 1, 200, ct);

        var recipients = managers.Items.Concat(admins.Items).Select(u => u.Id).Distinct();
        foreach (var userId in recipients)
        {
            await _mediator.Send(new CreateNotificationCommand(
                userId, NotificationType.TicketEscalated, title, body, "ticket", ticket.Id), ct);
        }
    }
}
