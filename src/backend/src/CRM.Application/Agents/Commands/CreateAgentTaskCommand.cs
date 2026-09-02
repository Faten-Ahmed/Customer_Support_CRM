using CRM.Application.Agents.DTOs;
using CRM.Domain.Agents;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record CreateAgentTaskCommand(
    Guid AgentId,
    string Title,
    string? Description,
    AgentTaskPriority Priority,
    DateTime? DueAt,
    Guid? TicketId,
    Guid? CustomerId)
    : IRequest<AgentTaskDto>;

public class CreateAgentTaskCommandHandler
    : IRequestHandler<CreateAgentTaskCommand, AgentTaskDto>
{
    private const int MaxIncompleteTasks = 200;
    private readonly IAgentTaskRepository _tasks;

    public CreateAgentTaskCommandHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task<AgentTaskDto> Handle(
        CreateAgentTaskCommand cmd, CancellationToken ct)
    {
        int count = await _tasks.CountIncompleteAsync(cmd.AgentId, ct);
        if (count >= MaxIncompleteTasks)
            throw new ValidationException("MAX_TASKS_REACHED: Maximum 200 incomplete tasks allowed.",
                new[] { new ValidationFailure("Tasks",
                    "Maximum 200 incomplete tasks reached.", "MAX_TASKS_REACHED") });

        var task = AgentTask.Create(
            cmd.AgentId, cmd.Title, cmd.Description,
            cmd.Priority, cmd.DueAt, cmd.TicketId, cmd.CustomerId);

        await _tasks.AddAsync(task, ct);
        await _tasks.SaveChangesAsync(ct);

        return Map(task);
    }

    internal static AgentTaskDto Map(AgentTask t)
        => new(t.Id, t.Title, t.Description, t.Priority.ToString(), t.Status.ToString(),
               t.DueAt, t.DueAt < DateTime.UtcNow && t.Status != AgentTaskStatus.Completed,
               t.TicketId, t.CustomerId, t.CreatedAt, t.CompletedAt);
}
