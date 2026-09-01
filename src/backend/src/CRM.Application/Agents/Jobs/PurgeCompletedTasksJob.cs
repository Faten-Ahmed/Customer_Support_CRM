using CRM.Domain.Agents;

namespace CRM.Application.Agents.Jobs;

public class PurgeCompletedTasksJob
{
    private readonly IAgentTaskRepository _tasks;

    public PurgeCompletedTasksJob(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task Execute(CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(-30);
        await _tasks.PurgeCompletedOlderThanAsync(threshold, ct);
    }
}
