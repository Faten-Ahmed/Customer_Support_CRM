using CRM.Domain.Common;
using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub — real implementation follows in BE infrastructure tasks.
public class TicketHistoryRepository : ITicketHistoryRepository
{
    public Task<PagedResult<TicketHistoryProjection>> ListByTicketAsync(
        Guid ticketId, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<TicketHistoryProjection>(
            new List<TicketHistoryProjection>(), 0, page, pageSize));
}
