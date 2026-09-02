using CRM.Application.Agents.DTOs;
using CRM.Domain.Agents;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record UpdateAgentTaskCommand(
    Guid TaskId,
    Guid AgentId,
    string? Title,
    string? Description,
    AgentTaskPriority? Priority,
    AgentTaskStatus? Status,
    DateTime? DueAt)
    : IRequest<AgentTaskDto>;

public class UpdateAgentTaskCommandHandler
    : IRequestHandler<UpdateAgentTaskCommand, AgentTaskDto>
{
    private readonly IAgentTaskRepository _tasks;

    public UpdateAgentTaskCommandHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task<AgentTaskDto> Handle(
        UpdateAgentTaskCommand cmd, CancellationToken ct)
    {
        var task = await _tasks.FindByIdAsync(cmd.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (task.AgentId != cmd.AgentId)
            throw new UnauthorizedAccessException("You can only update your own tasks.");

        task.Update(cmd.Title, cmd.Description, cmd.Priority, cmd.Status, cmd.DueAt);
        await _tasks.SaveChangesAsync(ct);

        return CreateAgentTaskCommandHandler.Map(task);
    }
}
