using CRM.Application.Tickets.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record ListTicketsQuery(
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? CustomerId,
    Guid? AssignedToUserId,
    Guid? CategoryId,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDesc,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    string? Search = null) : IRequest<PagedResult<TicketSummaryDto>>;

public class ListTicketsQueryHandler
    : IRequestHandler<ListTicketsQuery, PagedResult<TicketSummaryDto>>
{
    private readonly ITicketRepository _tickets;

    public ListTicketsQueryHandler(ITicketRepository tickets) => _tickets = tickets;

    public async Task<PagedResult<TicketSummaryDto>> Handle(
        ListTicketsQuery query, CancellationToken ct)
    {
        var isAgent = query.RequestingUserRole == UserRole.Agent;

        var filter = new TicketFilter(
            query.Status, query.Priority, query.CustomerId,
            AssignedToUserId: isAgent ? null : query.AssignedToUserId,
            AgentQueueUserId: isAgent ? query.RequestingUserId : null,
            query.CategoryId, query.Search,
            query.Page, query.PageSize, query.SortBy, query.SortDesc);

        var projected = await _tickets.ListAsync(filter, ct);

        var items = projected.Items.Select(p => new TicketSummaryDto(
            p.Id, p.TicketNumber, p.CustomerId, p.CustomerName,
            p.Subject, p.Status, p.Priority, p.Channel,
            p.AssignedToUserId, p.AssignedToName,
            p.CreatedAt, p.UpdatedAt)).ToList();

        return new PagedResult<TicketSummaryDto>(items, projected.TotalCount, projected.Page, projected.PageSize);
    }
}
