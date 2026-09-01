using CRM.Application.Agents.Commands;
using CRM.Application.Agents.DTOs;
using CRM.Domain.Agents;
using CRM.Domain.Common;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record ListMyTasksQuery(
    Guid AgentId,
    AgentTaskStatus? Status,
    AgentTaskPriority? Priority,
    Guid? TicketId,
    bool OverdueOnly,
    int Page,
    int PageSize)
    : IRequest<PagedResult<AgentTaskDto>>;

public class ListMyTasksQueryHandler
    : IRequestHandler<ListMyTasksQuery, PagedResult<AgentTaskDto>>
{
    private readonly IAgentTaskRepository _tasks;

    public ListMyTasksQueryHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task<PagedResult<AgentTaskDto>> Handle(
        ListMyTasksQuery query, CancellationToken ct)
    {
        var paged = await _tasks.ListAsync(
            query.AgentId, query.Status, query.Priority, query.TicketId,
            query.OverdueOnly, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(CreateAgentTaskCommandHandler.Map)
            .ToList();

        return new PagedResult<AgentTaskDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
