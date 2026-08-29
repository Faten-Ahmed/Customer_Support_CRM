using CRM.Application.Common;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;

namespace CRM.Application.Sla.Jobs;

public class SlaMonitorJob
{
    private readonly ITicketSlaRepository _slaRepo;
    private readonly IBusinessHoursRepository _hoursRepo;
    private readonly INotificationService _notifications;
    private readonly ITicketRepository _tickets;

    public SlaMonitorJob(
        ITicketSlaRepository slaRepo,
        IBusinessHoursRepository hoursRepo,
        INotificationService notifications,
        ITicketRepository tickets)
    {
        _slaRepo = slaRepo;
        _hoursRepo = hoursRepo;
        _notifications = notifications;
        _tickets = tickets;
    }

    public async Task Execute(CancellationToken ct = default)
    {
        var activeSlas = await _slaRepo.ListActiveAsync(ct);
        if (!activeSlas.Any()) return;

        var globalHours = await _hoursRepo.FindGlobalAsync(ct);
        var now = DateTime.UtcNow;

        foreach (var sla in activeSlas)
        {
            if (sla.ClockPausedAt.HasValue) continue;

            await ProcessSlaClock(sla, now, globalHours, ct);
        }

        await _slaRepo.SaveChangesAsync(ct);
    }

    private async Task ProcessSlaClock(
        TicketSla sla, DateTime now, BusinessHours? hours, CancellationToken ct)
    {
        var newTier = ComputeBreachTier(sla, now, hours);

        if (newTier > sla.BreachTier)
        {
            sla.UpdateBreachTier(newTier);

            var ticket = await _tickets.FindByIdAsync(sla.TicketId, ct);
            await _notifications.SendSlaBreachAlertAsync(
                sla.TicketId, newTier,
                ticket?.AssignedToUserId,
                ticket?.DepartmentId,
                ct);
        }
    }

    private static SlaBreachTier ComputeBreachTier(
        TicketSla sla, DateTime now, BusinessHours? hours)
    {
        if (!sla.ResolutionDue.HasValue) return SlaBreachTier.None;

        var totalMinutes = (sla.ResolutionDue.Value - sla.ClockStartedAt).TotalMinutes
            - sla.AccumulatedPauseMinutes;
        if (totalMinutes <= 0) return SlaBreachTier.CriticalBreach;

        var elapsed = (now - sla.ClockStartedAt).TotalMinutes - sla.AccumulatedPauseMinutes;
        var percentElapsed = (elapsed / totalMinutes) * 100;

        return percentElapsed switch
        {
            >= 200 => SlaBreachTier.CriticalBreach,
            >= 100 => SlaBreachTier.Breach,
            >= 80 => SlaBreachTier.Warning,
            _ => SlaBreachTier.None
        };
    }
}
