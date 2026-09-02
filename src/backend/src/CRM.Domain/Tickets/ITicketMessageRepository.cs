using CRM.Domain.Common;

namespace CRM.Domain.Tickets;

public record TicketMessageProjection(
    Guid Id,
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid? AuthorUserId,
    string? AuthorName,
    Guid? AuthorCustomerId,
    DateTime CreatedAt);

public interface ITicketMessageRepository
{
    Task<TicketMessage?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TicketMessage message, CancellationToken ct = default);
    Task<PagedResult<TicketMessageProjection>> ListByTicketAsync(
        Guid ticketId,
        bool includeInternal,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
