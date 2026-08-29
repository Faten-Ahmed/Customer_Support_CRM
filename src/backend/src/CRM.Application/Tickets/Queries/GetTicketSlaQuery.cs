using CRM.Application.Tickets.DTOs;
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketSlaQuery(Guid TicketId) : IRequest<TicketSlaDto>;

public class GetTicketSlaQueryHandler : IRequestHandler<GetTicketSlaQuery, TicketSlaDto>
{
    private readonly ITicketSlaRepository _sla;

    public GetTicketSlaQueryHandler(ITicketSlaRepository sla) => _sla = sla;

    public async Task<TicketSlaDto> Handle(GetTicketSlaQuery query, CancellationToken ct)
    {
        var sla = await _sla.FindByTicketIdAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"No SLA record found for ticket {query.TicketId}.");

        return new TicketSlaDto(
            TicketId: sla.TicketId,
            ClockStartedAt: sla.ClockStartedAt,
            FirstResponseDue: sla.FirstResponseDue,
            ResolutionDue: sla.ResolutionDue,
            FirstResponseAt: sla.FirstResponseAt,
            FirstResponseBreached: sla.FirstResponseBreached,
            ResolutionBreached: sla.ResolutionBreached,
            BreachTier: sla.BreachTier.ToString(),
            AccumulatedPauseMinutes: sla.AccumulatedPauseMinutes,
            IsPaused: sla.ClockPausedAt.HasValue);
    }
}
