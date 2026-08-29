namespace CRM.Application.Tickets.DTOs;

public record TicketSlaDto(
    Guid TicketId,
    DateTime ClockStartedAt,
    DateTime? FirstResponseDue,
    DateTime? ResolutionDue,
    DateTime? FirstResponseAt,
    bool FirstResponseBreached,
    bool ResolutionBreached,
    string BreachTier,
    int AccumulatedPauseMinutes,
    bool IsPaused);
