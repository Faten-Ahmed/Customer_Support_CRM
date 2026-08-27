namespace CRM.Domain.Tickets;

public interface ITicketRepository
{
    Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default);
}
