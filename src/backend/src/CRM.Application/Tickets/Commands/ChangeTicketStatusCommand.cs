using CRM.Application.Sla;
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
    private readonly IBusinessHoursRepository _businessHours;

    public ChangeTicketStatusCommandHandler(
        ITicketRepository tickets,
        ITicketSlaRepository slaRepo,
        IBusinessHoursRepository businessHours)
    {
        _tickets = tickets;
        _slaRepo = slaRepo;
        _businessHours = businessHours;
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
            {
                sla.PauseClock();
            }
            else if (previousStatus == TicketStatus.OnHold && sla.ClockPausedAt.HasValue)
            {
                BusinessHours? hours = null;
                if (ticket.DepartmentId.HasValue)
                    hours = await _businessHours.FindByDepartmentAsync(ticket.DepartmentId.Value, ct);
                hours ??= await _businessHours.FindGlobalAsync(ct);

                var businessPauseMinutes = hours is not null
                    ? BusinessTimeCalculator.ElapsedBusinessMinutes(sla.ClockPausedAt.Value, DateTime.UtcNow, hours)
                    : (int)(DateTime.UtcNow - sla.ClockPausedAt.Value).TotalMinutes;

                sla.ResumeClock(businessPauseMinutes);
            }
        }

        await _tickets.SaveChangesAsync(ct);
        if (sla is not null)
            await _slaRepo.SaveChangesAsync(ct);
    }
}
