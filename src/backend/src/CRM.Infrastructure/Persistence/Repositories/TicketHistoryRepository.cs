using CRM.Domain.Common;
using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class TicketHistoryRepository : ITicketHistoryRepository
{
    private readonly AppDbContext _db;
    public TicketHistoryRepository(AppDbContext db) => _db = db;

    public async Task<PagedResult<TicketHistoryProjection>> ListByTicketAsync(
        Guid ticketId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.TicketHistory.Where(h => h.TicketId == ticketId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(h => h.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new TicketHistoryProjection(
                h.Id,
                h.FieldChanged,
                h.OldValue,
                h.NewValue,
                h.ChangedByUserId,
                _db.Users
                    .Where(u => u.Id == h.ChangedByUserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault() ?? "System",
                h.ChangedAt))
            .ToListAsync(ct);

        return new PagedResult<TicketHistoryProjection>(items, total, page, pageSize);
    }
}
