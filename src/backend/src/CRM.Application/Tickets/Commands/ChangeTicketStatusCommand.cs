using CRM.Application.Tickets.Services;
using CRM.Domain.Sla;
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
    private readonly ITicketSlaRepository _slaRepo;

    public ChangeTicketStatusCommandHandler(
        ITicketRepository tickets,
        ITicketSlaRepository slaRepo)
    {
        _tickets = tickets;
        _slaRepo = slaRepo;
    }

    public async Task Handle(ChangeTicketStatusCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (!TicketStateMachine.IsValidTransition(ticket.Status, cmd.NewStatus))
            throw new InvalidOperationException(
                $"Cannot transition from {ticket.Status} to {cmd.NewStatus}.");

        var previousStatus = ticket.Status;
        ticket.ChangeStatus(cmd.NewStatus, cmd.ChangedByUserId);

        var sla = await _slaRepo.FindByTicketIdAsync(cmd.TicketId, ct);
        if (sla is not null)
        {
            if (cmd.NewStatus == TicketStatus.OnHold)
                sla.PauseClock();
            else if (previousStatus == TicketStatus.OnHold)
                sla.ResumeClock();
        }

        await _tickets.SaveChangesAsync(ct);
        if (sla is not null)
            await _slaRepo.SaveChangesAsync(ct);
    }
}
