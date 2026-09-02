namespace CRM.Application.Tickets.DTOs;

public record TicketSlaDto(
    bool IsPaused,
    SlaClock FirstResponse,
    SlaClock Resolution);

public record SlaClock(
    string? DueAt,
    double ElapsedPercent,
    bool Breached,
    string RemainingLabel);
