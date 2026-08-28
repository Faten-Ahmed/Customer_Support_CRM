using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record ChangeTicketStatusCommand(
    Guid TicketId,
    TicketStatus NewStatus,
    Guid ChangedByUserId) : IRequest;

public class ChangeTicketStatusCommandHandler : IRequestHandler<ChangeTicketStatusCommand>
{
    private readonly ITicketRepository _tickets;

    public ChangeTicketStatusCommandHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task Handle(ChangeTicketStatusCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (!TicketStateMachine.IsValidTransition(ticket.Status, cmd.NewStatus))
            throw new InvalidOperationException(
                $"Cannot transition from {ticket.Status} to {cmd.NewStatus}.");

        ticket.ChangeStatus(cmd.NewStatus, cmd.ChangedByUserId);
        await _tickets.SaveChangesAsync(ct);
    }
}
