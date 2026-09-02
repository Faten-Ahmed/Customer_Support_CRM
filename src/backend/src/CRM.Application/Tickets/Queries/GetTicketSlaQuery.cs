using CRM.Application.Sla;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketSlaQuery(Guid TicketId) : IRequest<TicketSlaDto>;

public class GetTicketSlaQueryHandler : IRequestHandler<GetTicketSlaQuery, TicketSlaDto>
{
    private readonly ITicketSlaRepository _slaRepo;
    private readonly ITicketRepository _tickets;
    private readonly ISlaPolicyRepository _policies;
    private readonly IBusinessHoursRepository _businessHours;

    public GetTicketSlaQueryHandler(
        ITicketSlaRepository slaRepo,
        ITicketRepository tickets,
        ISlaPolicyRepository policies,
        IBusinessHoursRepository businessHours)
    {
        _slaRepo = slaRepo;
        _tickets = tickets;
        _policies = policies;
        _businessHours = businessHours;
    }

    private static readonly SlaClock _noClock = new(null, 0, false, "—");

    public async Task<TicketSlaDto> Handle(GetTicketSlaQuery query, CancellationToken ct)
    {
        var sla = await _slaRepo.FindByTicketIdAsync(query.TicketId, ct);
        if (sla is null)
            return new TicketSlaDto(IsPaused: false, FirstResponse: _noClock, Resolution: _noClock);

        var ticket = await _tickets.FindByIdAsync(query.TicketId, ct);

        BusinessHours? hours = null;
        if (ticket?.DepartmentId.HasValue == true)
            hours = await _businessHours.FindByDepartmentAsync(ticket.DepartmentId.Value, ct);
        hours ??= await _businessHours.FindGlobalAsync(ct);

        var now = DateTime.UtcNow;
        // When paused, freeze elapsed time at the pause moment so the clock stops counting
        var effectiveCeiling = sla.ClockPausedAt ?? now;

        var firstResponse = ComputeClock(
            sla.ClockStartedAt, sla.FirstResponseDue,
            sla.FirstResponseAt ?? effectiveCeiling,
            sla.AccumulatedPauseMinutes, sla.FirstResponseBreached, hours);

        var resolution = ComputeClock(
            sla.ClockStartedAt, sla.ResolutionDue,
            effectiveCeiling, sla.AccumulatedPauseMinutes, sla.ResolutionBreached, hours);

        return new TicketSlaDto(
            IsPaused: sla.ClockPausedAt.HasValue,
            FirstResponse: firstResponse,
            Resolution: resolution);
    }

    private static SlaClock ComputeClock(
        DateTime clockStart, DateTime? due, DateTime effectiveNow,
        int accumulatedPauseMinutes, bool storedBreached, BusinessHours? hours)
    {
        if (due is null)
            return new SlaClock(null, 0, storedBreached, "—");

        int totalMinutes;
        int elapsedMinutes;

        if (hours is not null)
        {
            totalMinutes = BusinessTimeCalculator.ElapsedBusinessMinutes(clockStart, due.Value, hours);
            elapsedMinutes = BusinessTimeCalculator.ElapsedBusinessMinutes(clockStart, effectiveNow, hours)
                             - accumulatedPauseMinutes;
        }
        else
        {
            totalMinutes = (int)(due.Value - clockStart).TotalMinutes;
            elapsedMinutes = (int)(effectiveNow - clockStart).TotalMinutes - accumulatedPauseMinutes;
        }

        elapsedMinutes = Math.Max(0, elapsedMinutes);
        var remainingMinutes = totalMinutes - elapsedMinutes;
        var percent = totalMinutes > 0
            ? Math.Round((double)elapsedMinutes / totalMinutes * 100, 1)
            : 0;

        return new SlaClock(
            DueAt: due.Value.ToString("O"),
            ElapsedPercent: percent,
            Breached: storedBreached || percent >= 100,
            RemainingLabel: FormatMinutes(remainingMinutes));
    }

    private static string FormatMinutes(int minutes)
    {
        var negative = minutes < 0;
        var abs = Math.Abs(minutes);
        var h = abs / 60;
        var m = abs % 60;
        var label = h > 0 ? $"{h}h {m}m" : $"{m}m";
        return negative ? $"-{label}" : label;
    }
}
