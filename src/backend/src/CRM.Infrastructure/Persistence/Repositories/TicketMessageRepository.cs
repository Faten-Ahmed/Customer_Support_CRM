using CRM.Domain.Common;
using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub — real implementation follows in BE infrastructure tasks.
public class TicketMessageRepository : ITicketMessageRepository
{
    public Task AddAsync(TicketMessage message, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<PagedResult<TicketMessageProjection>> ListByTicketAsync(
        Guid ticketId, bool includeInternal, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<TicketMessageProjection>(
            new List<TicketMessageProjection>(), 0, page, pageSize));

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
