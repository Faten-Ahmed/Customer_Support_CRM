using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record EscalateTicketCommand(
    Guid TicketId,
    string Reason,
    Guid EscalatedByUserId) : IRequest;

public class EscalateTicketCommandHandler : IRequestHandler<EscalateTicketCommand>
{
    private readonly ITicketRepository _tickets;

    public EscalateTicketCommandHandler(ITicketRepository tickets) => _tickets = tickets;

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
    }
}
