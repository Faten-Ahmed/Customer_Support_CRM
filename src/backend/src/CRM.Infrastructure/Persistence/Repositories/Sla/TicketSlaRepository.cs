using CRM.Domain.Sla;
using CRM.Domain.Tickets.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories.Sla;

public class TicketSlaRepository : ITicketSlaRepository
{
    private readonly AppDbContext _db;
    public TicketSlaRepository(AppDbContext db) => _db = db;

    public Task<TicketSla?> FindByTicketIdAsync(Guid ticketId, CancellationToken ct = default)
        => _db.TicketSlas.FirstOrDefaultAsync(s => s.TicketId == ticketId, ct);

    public async Task<IReadOnlyList<TicketSla>> ListActiveAsync(CancellationToken ct = default)
        => await _db.TicketSlas
            .Where(s => s.ClockPausedAt == null)
            .Join(_db.Tickets,
                sla => sla.TicketId,
                t => t.Id,
                (sla, ticket) => new { sla, ticket })
            .Where(x => x.ticket.Status != TicketStatus.Closed
                     && x.ticket.Status != TicketStatus.Resolved)
            .Select(x => x.sla)
            .ToListAsync(ct);

    public async Task AddAsync(TicketSla sla, CancellationToken ct = default)
        => await _db.TicketSlas.AddAsync(sla, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
