using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record RecalculateSlaOnTransferCommand(
    Guid TicketId, Guid NewDepartmentId) : IRequest;

public class RecalculateSlaOnTransferCommandHandler
    : IRequestHandler<RecalculateSlaOnTransferCommand>
{
    private readonly ITicketSlaRepository _slaRepo;
    private readonly ISlaPolicyRepository _policies;
    private readonly IBusinessHoursRepository _businessHours;
    private readonly ITicketRepository _tickets;

    public RecalculateSlaOnTransferCommandHandler(
        ITicketSlaRepository slaRepo,
        ISlaPolicyRepository policies,
        IBusinessHoursRepository businessHours,
        ITicketRepository tickets)
    {
        _slaRepo = slaRepo;
        _policies = policies;
        _businessHours = businessHours;
        _tickets = tickets;
    }

    public async Task Handle(RecalculateSlaOnTransferCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        var sla = await _slaRepo.FindByTicketIdAsync(cmd.TicketId, ct);
        if (sla is null) return;

        var policy = await _policies.FindByDepartmentAndPriorityAsync(
            cmd.NewDepartmentId, ticket.Priority, ct)
            ?? await _policies.FindGlobalByPriorityAsync(ticket.Priority, ct);

        if (policy is null) return;

        var hours = await _businessHours.FindByDepartmentAsync(cmd.NewDepartmentId, ct)
            ?? await _businessHours.FindGlobalAsync(ct);

        var now = DateTime.UtcNow;
        var elapsedMinutes = hours is not null
            ? BusinessTimeCalculator.ElapsedBusinessMinutes(sla.ClockStartedAt, now, hours)
            : (int)(now - sla.ClockStartedAt).TotalMinutes;

        elapsedMinutes -= sla.AccumulatedPauseMinutes;

        var remainingFirstResponse = Math.Max(0, policy.FirstResponseMinutes - elapsedMinutes);
        var remainingResolution = Math.Max(0, policy.ResolutionMinutes - elapsedMinutes);

        DateTime? newFirstResponseDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(now, remainingFirstResponse, hours)
            : now.AddMinutes(remainingFirstResponse);

        DateTime? newResolutionDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(now, remainingResolution, hours)
            : now.AddMinutes(remainingResolution);

        if (remainingFirstResponse == 0) newFirstResponseDue = now;
        if (remainingResolution == 0) newResolutionDue = now;

        sla.RecalculateDeadlines(policy.Id, newFirstResponseDue, newResolutionDue);

        await _slaRepo.SaveChangesAsync(ct);
    }
}
