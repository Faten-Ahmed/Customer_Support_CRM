using CRM.Domain.Agents;
using CRM.Domain.Common;
using CRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Agents;

public class AgentTaskRepository : IAgentTaskRepository
{
    private readonly AppDbContext _db;

    public AgentTaskRepository(AppDbContext db) => _db = db;

    public Task<AgentTask?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.AgentTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<PagedResult<AgentTask>> ListAsync(
        Guid agentId,
        AgentTaskStatus? status,
        AgentTaskPriority? priority,
        Guid? ticketId,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.AgentTasks
            .Where(t => t.AgentId == agentId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (ticketId.HasValue)
            query = query.Where(t => t.TicketId == ticketId.Value);

        if (overdueOnly)
            query = query.Where(t => t.DueAt < DateTime.UtcNow && t.Status != AgentTaskStatus.Completed);

        var total = await query.CountAsync(ct);

        // Sort: incomplete first, then dueAt ASC, then createdAt ASC
        var items = await query
            .OrderBy(t => t.Status == AgentTaskStatus.Completed ? 1 : 0)
            .ThenBy(t => t.DueAt ?? DateTime.MaxValue)
            .ThenBy(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AgentTask>(items, total, page, pageSize);
    }

    public Task<int> CountIncompleteAsync(Guid agentId, CancellationToken ct = default)
        => _db.AgentTasks.CountAsync(t =>
            t.AgentId == agentId && t.Status != AgentTaskStatus.Completed, ct);

    public async Task AddAsync(AgentTask task, CancellationToken ct = default)
        => await _db.AgentTasks.AddAsync(task, ct);

    public Task RemoveAsync(AgentTask task, CancellationToken ct = default)
    {
        _db.AgentTasks.Remove(task);
        return Task.CompletedTask;
    }

    public async Task<int> PurgeCompletedOlderThanAsync(DateTime threshold, CancellationToken ct = default)
    {
        var toDelete = await _db.AgentTasks
            .Where(t => t.Status == AgentTaskStatus.Completed
                        && t.CompletedAt.HasValue
                        && t.CompletedAt.Value < threshold)
            .ToListAsync(ct);

        _db.AgentTasks.RemoveRange(toDelete);
        await _db.SaveChangesAsync(ct);
        return toDelete.Count;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
