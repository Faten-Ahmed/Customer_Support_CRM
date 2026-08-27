using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub implementation until the Ticket domain (US-BE-019+) is implemented.
public class TicketRepository : ITicketRepository
{
    public Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default)
        => Task.FromResult(false);
}
