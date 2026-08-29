using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record StartSlaClockCommand(Guid TicketId) : IRequest;

public class StartSlaClockCommandHandler : IRequestHandler<StartSlaClockCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly ISlaPolicyRepository _policies;
    private readonly IBusinessHoursRepository _businessHours;
    private readonly ITicketSlaRepository _slaRepo;

    public StartSlaClockCommandHandler(
        ITicketRepository tickets,
        ISlaPolicyRepository policies,
        IBusinessHoursRepository businessHours,
        ITicketSlaRepository slaRepo)
    {
        _tickets = tickets;
        _policies = policies;
        _businessHours = businessHours;
        _slaRepo = slaRepo;
    }

    public async Task Handle(StartSlaClockCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        SlaPolicy? policy = null;
        BusinessHours? hours = null;

        if (ticket.DepartmentId.HasValue)
        {
            policy = await _policies.FindByDepartmentAndPriorityAsync(
                ticket.DepartmentId.Value, ticket.Priority, ct);
            hours = await _businessHours.FindByDepartmentAsync(ticket.DepartmentId.Value, ct);
        }

        policy ??= await _policies.FindGlobalByPriorityAsync(ticket.Priority, ct);
        hours ??= await _businessHours.FindGlobalAsync(ct);

        if (policy is null) return;

        var start = ticket.CreatedAt;
        DateTime? firstResponseDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(start, policy.FirstResponseMinutes, hours)
            : start.AddMinutes(policy.FirstResponseMinutes);

        DateTime? resolutionDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(start, policy.ResolutionMinutes, hours)
            : start.AddMinutes(policy.ResolutionMinutes);

        var sla = TicketSla.Create(ticket.Id, policy.Id, start, firstResponseDue, resolutionDue);

        await _slaRepo.AddAsync(sla, ct);
        await _slaRepo.SaveChangesAsync(ct);
    }
}
