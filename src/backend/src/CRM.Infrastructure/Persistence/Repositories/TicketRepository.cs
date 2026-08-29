using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _db;
    public TicketRepository(AppDbContext db) => _db = db;

    public Task<Ticket?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Ticket?> FindByIdDetailedAsync(Guid id, CancellationToken ct = default)
        => _db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
        => await _db.Tickets.AddAsync(ticket, ct);

    public Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default)
        => _db.Tickets.AnyAsync(t =>
            t.CustomerId == customerId &&
            t.Status != TicketStatus.Resolved &&
            t.Status != TicketStatus.Closed, ct);

    public async Task<PagedResult<TicketListProjection>> ListAsync(
        TicketFilter filter, CancellationToken ct = default)
    {
        var query = _db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);
        if (filter.CustomerId.HasValue)
            query = query.Where(t => t.CustomerId == filter.CustomerId.Value);
        if (filter.AgentQueueUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == filter.AgentQueueUserId.Value || t.AssignedToUserId == null);
        else if (filter.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);
        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(t =>
                t.TicketNumber.Contains(filter.Search) ||
                t.Subject.Contains(filter.Search));

        var total = await query.CountAsync(ct);

        query = filter.SortBy switch
        {
            "priority" => filter.SortDesc
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "status" => filter.SortDesc
                ? query.OrderByDescending(t => t.Status)
                : query.OrderBy(t => t.Status),
            _ => filter.SortDesc
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TicketListProjection(
                t.Id, t.TicketNumber, t.CustomerId,
                t.Customer!.FullName,
                t.Subject, t.Status.ToString(), t.Priority.ToString(),
                t.Channel.ToString(),
                t.AssignedToUserId,
                t.AssignedTo != null ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName : null,
                t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);

        return new PagedResult<TicketListProjection>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<PagedResult<CustomerTicketProjection>> ListByCustomerAsync(
        Guid customerId,
        string? status,
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .Where(t => t.CustomerId == customerId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TicketStatus>(status, out var parsedStatus))
            query = query.Where(t => t.Status == parsedStatus);

        if (departmentIds is { Count: > 0 })
            query = query.Where(t => t.DepartmentId.HasValue &&
                                     departmentIds.Contains(t.DepartmentId.Value));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new CustomerTicketProjection(
                t.Id, t.TicketNumber, t.Subject,
                t.Status.ToString(), t.Priority.ToString(),
                t.CreatedAt, null))
            .ToListAsync(ct);

        return new PagedResult<CustomerTicketProjection>(items, total, page, pageSize);
    }

    public Task<int> CountOpenForDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => Task.FromResult(0);

    public Task<int> CountOpenForCategoryAsync(Guid categoryId, CancellationToken ct = default)
        => Task.FromResult(0);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
