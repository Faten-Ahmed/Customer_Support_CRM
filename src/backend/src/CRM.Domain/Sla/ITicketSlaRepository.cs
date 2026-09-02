namespace CRM.Domain.Sla;

public interface ITicketSlaRepository
{
    Task<TicketSla?> FindByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketSla>> ListActiveAsync(CancellationToken ct = default);
    Task AddAsync(TicketSla sla, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
