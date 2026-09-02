using CRM.Domain.Agents;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record DeleteAgentTaskCommand(Guid TaskId, Guid AgentId) : IRequest;

public class DeleteAgentTaskCommandHandler : IRequestHandler<DeleteAgentTaskCommand>
{
    private readonly IAgentTaskRepository _tasks;

    public DeleteAgentTaskCommandHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task Handle(DeleteAgentTaskCommand cmd, CancellationToken ct)
    {
        var task = await _tasks.FindByIdAsync(cmd.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (task.AgentId != cmd.AgentId)
            throw new UnauthorizedAccessException("You can only delete your own tasks.");

        await _tasks.RemoveAsync(task, ct);
        await _tasks.SaveChangesAsync(ct);
    }
}
