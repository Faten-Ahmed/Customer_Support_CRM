namespace CRM.Application.Tickets.DTOs;

public record TicketHistoryEntryDto(
    string FieldChanged,
    string? OldValue,
    string? NewValue,
    string ChangedByName,
    DateTime ChangedAt);
