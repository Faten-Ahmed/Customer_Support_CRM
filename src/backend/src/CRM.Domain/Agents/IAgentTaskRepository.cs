using CRM.Domain.Common;

namespace CRM.Domain.Agents;

public interface IAgentTaskRepository
{
    Task<AgentTask?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<AgentTask>> ListAsync(
        Guid agentId,
        AgentTaskStatus? status,
        AgentTaskPriority? priority,
        Guid? ticketId,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<int> CountIncompleteAsync(Guid agentId, CancellationToken ct = default);
    Task AddAsync(AgentTask task, CancellationToken ct = default);
    Task RemoveAsync(AgentTask task, CancellationToken ct = default);
    Task<int> PurgeCompletedOlderThanAsync(DateTime threshold, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
