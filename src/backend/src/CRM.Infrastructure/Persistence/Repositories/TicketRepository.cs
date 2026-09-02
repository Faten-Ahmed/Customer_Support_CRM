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

    public async Task<string?> GetDepartmentNameAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.Departments
            .Where(d => d.Id == departmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<bool> IsDepartmentActiveAsync(Guid departmentId, CancellationToken ct = default)
        => await _db.Departments
            .AnyAsync(d => d.Id == departmentId && d.IsActive, ct);

    public async Task<string?> GetCategoryNameAsync(Guid categoryId, CancellationToken ct = default)
        => await _db.TicketCategories
            .Where(c => c.Id == categoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<Ticket>> ListUnassignedAsync(
        IReadOnlyList<Guid>? departmentIds,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .Where(t => t.Status == TicketStatus.New && t.AssignedToUserId == null)
            .AsQueryable();

        if (departmentIds is { Count: > 0 })
            query = query.Where(t => t.DepartmentId.HasValue &&
                                     departmentIds.Contains(t.DepartmentId.Value));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Ticket>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<Ticket>> FindResolvedWithNoCustomerReplyAsync(
        DateTime resolvedBefore,
        CancellationToken ct = default)
    {
        var resolvedStatus = TicketStatus.Resolved;

        var tickets = await _db.Tickets
            .Where(t => t.Status == resolvedStatus
                        && t.ResolvedAt.HasValue
                        && t.ResolvedAt.Value < resolvedBefore
                        && !_db.TicketMessages.Any(m =>
                            m.TicketId == t.Id
                            && m.AuthorCustomerId.HasValue
                            && m.CreatedAt > t.ResolvedAt.Value))
            .ToListAsync(ct);

        return tickets;
    }

    public async Task<PagedResult<MyTicketProjection>> ListAssignedToAgentAsync(
        Guid agentId,
        AgentTicketFilter filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Tickets
            .Include(t => t.Customer)
            .Where(t => t.AssignedToUserId == agentId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<TicketStatus>(filter.Status, out var parsedStatus))
            query = query.Where(t => t.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(filter.Priority) &&
            Enum.TryParse<TicketPriority>(filter.Priority, out var parsedPriority))
            query = query.Where(t => t.Priority == parsedPriority);

        if (filter.DepartmentId.HasValue)
            query = query.Where(t => t.DepartmentId == filter.DepartmentId.Value);

        var total = await query.CountAsync(ct);

        var sortBy = filter.SortBy ?? "Priority";
        var sortDesc = (filter.SortDir ?? "desc").Equals("desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy.ToLower() switch
        {
            "priority" => sortDesc
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
            "createdat" => sortDesc
                ? query.OrderByDescending(t => t.CreatedAt)
                : query.OrderBy(t => t.CreatedAt),
            _ => sortDesc
                ? query.OrderByDescending(t => t.Priority)
                : query.OrderBy(t => t.Priority),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new MyTicketProjection(
                t.Id, t.TicketNumber, t.CustomerId,
                t.Customer!.FullName,
                t.Subject, t.Status.ToString(), t.Priority.ToString(),
                t.Channel.ToString(),
                t.DepartmentId, t.CategoryId, t.CreatedAt,
                null, "None", null))
            .ToListAsync(ct);

        return new PagedResult<MyTicketProjection>(items, total, page, pageSize);
    }

    public async Task<TicketRenderContext?> GetRenderContextAsync(
        Guid ticketId,
        CancellationToken ct = default)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

        if (ticket is null) return null;

        var deptName = ticket.DepartmentId.HasValue
            ? await GetDepartmentNameAsync(ticket.DepartmentId.Value, ct) ?? "Unknown"
            : "Unknown";

        var agentName = ticket.AssignedTo is not null
            ? $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}"
            : "Unassigned";

        var customerName = ticket.Customer?.FullName ?? "Unknown";

        return new TicketRenderContext(ticket.TicketNumber, customerName, agentName, deptName);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
