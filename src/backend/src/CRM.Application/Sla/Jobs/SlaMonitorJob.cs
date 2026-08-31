using CRM.Application.Common;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.Services;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Sla.Jobs;

public class SlaMonitorJob
{
    private readonly ITicketSlaRepository _slaRepo;
    private readonly IBusinessHoursRepository _hoursRepo;
    private readonly ISlaPolicyRepository _policies;
    private readonly INotificationService _notifications;
    private readonly ITicketRepository _tickets;
    private readonly IMediator _mediator;

    public SlaMonitorJob(
        ITicketSlaRepository slaRepo,
        IBusinessHoursRepository hoursRepo,
        ISlaPolicyRepository policies,
        INotificationService notifications,
        ITicketRepository tickets,
        IMediator mediator)
    {
        _slaRepo = slaRepo;
        _hoursRepo = hoursRepo;
        _policies = policies;
        _notifications = notifications;
        _tickets = tickets;
        _mediator = mediator;
    }

    public async Task Execute(CancellationToken ct = default)
    {
        var activeSlas = await _slaRepo.ListActiveAsync(ct);
        if (!activeSlas.Any()) return;

        var globalHours = await _hoursRepo.FindGlobalAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var sla in activeSlas)
        {
            try
            {
                var ticket = await _tickets.FindByIdAsync(sla.TicketId, ct);
                if (ticket is null) continue;

                BusinessHours? hours = null;
                if (ticket.DepartmentId.HasValue)
                    hours = await _hoursRepo.FindByDepartmentAsync(ticket.DepartmentId.Value, ct);
                hours ??= globalHours;

                var policy = await _policies.FindByIdAsync(sla.SlaPolicyId, ct);
                if (policy is null) continue;

                await ProcessFirstResponseClock(sla, ticket, policy, now, hours, ct);
                await ProcessResolutionClock(sla, ticket, policy, now, hours, ct);
            }
            catch (Exception)
            {
                // Don't let one ticket failure stop the rest of the batch
            }
        }

        await _slaRepo.SaveChangesAsync(ct);
    }

    private async Task ProcessFirstResponseClock(
        TicketSla sla, Ticket ticket, SlaPolicy policy,
        DateTime now, BusinessHours? hours, CancellationToken ct)
    {
        if (sla.FirstResponseAt.HasValue) return;
        if (!sla.FirstResponseDue.HasValue) return;

        var elapsed = ComputeElapsed(sla, now, hours);
        var total = ComputeTotal(sla.ClockStartedAt, sla.FirstResponseDue.Value, hours);
        if (total <= 0) return;

        var percent = (double)elapsed / total * 100;
        var newTier = ComputeTier(percent, policy);

        if (newTier > sla.FirstResponseBreachTier)
        {
            sla.UpdateFirstResponseBreachTier(newTier);
            await _notifications.SendSlaBreachAlertAsync(
                sla.TicketId, newTier, ticket.AssignedToUserId, ticket.DepartmentId, ct);
        }
    }

    private async Task ProcessResolutionClock(
        TicketSla sla, Ticket ticket, SlaPolicy policy,
        DateTime now, BusinessHours? hours, CancellationToken ct)
    {
        if (!sla.ResolutionDue.HasValue) return;

        var elapsed = ComputeElapsed(sla, now, hours);
        var total = ComputeTotal(sla.ClockStartedAt, sla.ResolutionDue.Value, hours);
        if (total <= 0) return;

        var percent = (double)elapsed / total * 100;
        var newTier = ComputeTier(percent, policy);

        if (newTier > sla.BreachTier)
        {
            sla.UpdateBreachTier(newTier);
            await _notifications.SendSlaBreachAlertAsync(
                sla.TicketId, newTier, ticket.AssignedToUserId, ticket.DepartmentId, ct);

            if (newTier == SlaBreachTier.CriticalBreach &&
                ticket.Status != TicketStatus.Escalated &&
                TicketStateMachine.IsValidTransition(ticket.Status, TicketStatus.Escalated))
            {
                await _mediator.Send(new EscalateTicketCommand(
                    sla.TicketId,
                    "SLA Critical Breach — auto-escalated",
                    Guid.Empty), ct);
            }
        }
    }

    private static int ComputeElapsed(TicketSla sla, DateTime now, BusinessHours? hours)
    {
        var raw = hours is not null
            ? BusinessTimeCalculator.ElapsedBusinessMinutes(sla.ClockStartedAt, now, hours)
            : (int)(now - sla.ClockStartedAt).TotalMinutes;
        return Math.Max(0, raw - sla.AccumulatedPauseMinutes);
    }

    private static int ComputeTotal(DateTime start, DateTime due, BusinessHours? hours)
        => hours is not null
            ? BusinessTimeCalculator.ElapsedBusinessMinutes(start, due, hours)
            : (int)(due - start).TotalMinutes;

    private static SlaBreachTier ComputeTier(double percent, SlaPolicy policy)
    {
        if (percent >= policy.CriticalBreachThresholdPercent) return SlaBreachTier.CriticalBreach;
        if (percent >= policy.BreachThresholdPercent) return SlaBreachTier.Breach;
        if (percent >= policy.WarningThresholdPercent) return SlaBreachTier.Warning;
        return SlaBreachTier.None;
    }
}
