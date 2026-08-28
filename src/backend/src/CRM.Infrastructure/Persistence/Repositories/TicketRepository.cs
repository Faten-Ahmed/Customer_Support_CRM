using CRM.Domain.Common;
using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub — real implementation follows in US-BE-019+ infrastructure tasks.
public class TicketRepository : ITicketRepository
{
    public Task<Ticket?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Ticket?>(null);

    public Task<Ticket?> FindByIdDetailedAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Ticket?>(null);

    public Task AddAsync(Ticket ticket, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<PagedResult<TicketListProjection>> ListAsync(TicketFilter filter, CancellationToken ct = default)
        => Task.FromResult(new PagedResult<TicketListProjection>(
            new List<TicketListProjection>(), 0, filter.Page, filter.PageSize));

    public Task<PagedResult<CustomerTicketProjection>> ListByCustomerAsync(
        Guid customerId,
        string? status,
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default)
        => Task.FromResult(new PagedResult<CustomerTicketProjection>(
            new List<CustomerTicketProjection>(), 0, page, pageSize));

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
