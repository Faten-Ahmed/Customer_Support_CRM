using CRM.Domain.Common;
using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub implementation until the Ticket domain (US-BE-019+) is implemented.
public class TicketRepository : ITicketRepository
{
    public Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<PagedResult<CustomerTicketProjection>> ListByCustomerAsync(
        Guid customerId,
        string? status,
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => Task.FromResult(new PagedResult<CustomerTicketProjection>(
            new List<CustomerTicketProjection>(), 0, page, pageSize));
}
