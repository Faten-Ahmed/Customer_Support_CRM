using CRM.Domain.Common;
using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class TicketMessageRepository : ITicketMessageRepository
{
    private readonly AppDbContext _db;
    public TicketMessageRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(TicketMessage message, CancellationToken ct = default)
        => await _db.TicketMessages.AddAsync(message, ct);

    public async Task<PagedResult<TicketMessageProjection>> ListByTicketAsync(
        Guid ticketId, bool includeInternal, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.TicketMessages
            .Where(m => m.TicketId == ticketId)
            .Where(m => includeInternal || !m.IsInternal);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new TicketMessageProjection(
                m.Id,
                m.TicketId,
                m.Body,
                m.IsInternal,
                m.AuthorUserId,
                m.AuthorUserId != null
                    ? _db.Users
                        .Where(u => u.Id == m.AuthorUserId)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault()
                    : m.AuthorCustomerId != null
                        ? _db.Customers
                            .Where(c => c.Id == m.AuthorCustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault()
                        : null,
                m.AuthorCustomerId,
                m.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<TicketMessageProjection>(items, total, page, pageSize);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
