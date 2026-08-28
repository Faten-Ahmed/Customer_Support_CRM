using CRM.Domain.Common;

namespace CRM.Domain.Tickets;

public record TicketHistoryProjection(
    Guid Id,
    string FieldChanged,
    string? OldValue,
    string? NewValue,
    Guid ChangedByUserId,
    string ChangedByName,
    DateTime ChangedAt);

public interface ITicketHistoryRepository
{
    Task<PagedResult<TicketHistoryProjection>> ListByTicketAsync(
        Guid ticketId,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
