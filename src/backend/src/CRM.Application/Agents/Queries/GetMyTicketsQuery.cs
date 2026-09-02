using CRM.Application.Agents.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record GetMyTicketsQuery(
    Guid AgentId,
    string? Status,
    string? Priority,
    Guid? DepartmentId,
    int Page,
    int PageSize,
    string? SortBy,
    string? SortDir) : IRequest<PagedResult<MyTicketDto>>;

public class GetMyTicketsQueryHandler
    : IRequestHandler<GetMyTicketsQuery, PagedResult<MyTicketDto>>
{
    private readonly ITicketRepository _tickets;

    public GetMyTicketsQueryHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<PagedResult<MyTicketDto>> Handle(
        GetMyTicketsQuery query, CancellationToken ct)
    {
        var filter = new AgentTicketFilter(
            query.Status,
            query.Priority,
            query.DepartmentId,
            query.SortBy ?? "Priority",
            query.SortDir ?? "desc");

        var paged = await _tickets.ListAssignedToAgentAsync(
            query.AgentId, filter, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(p => new MyTicketDto(
                p.Id, p.TicketNumber, p.CustomerId, p.CustomerFullName,
                p.Subject, p.Status, p.Priority, p.Channel,
                p.DepartmentId, p.CategoryId, p.CreatedAt,
                p.ResolutionDue, p.SlaStatus, p.ResolutionRemainingMinutes))
            .ToList();

        return new PagedResult<MyTicketDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
