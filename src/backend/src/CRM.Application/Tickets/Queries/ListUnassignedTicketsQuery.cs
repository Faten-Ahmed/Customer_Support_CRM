using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record ListUnassignedTicketsQuery(
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<UnassignedTicketDto>>;

public class ListUnassignedTicketsQueryHandler
    : IRequestHandler<ListUnassignedTicketsQuery, PagedResult<UnassignedTicketDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public ListUnassignedTicketsQueryHandler(ITicketRepository tickets, IUserRepository users)
    {
        _tickets = tickets;
        _users = users;
    }

    public async Task<PagedResult<UnassignedTicketDto>> Handle(
        ListUnassignedTicketsQuery query, CancellationToken ct)
    {
        IReadOnlyList<Guid>? departmentIds = null;
        if (query.RequestingUserRole == UserRole.Agent)
            departmentIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);

        var paged = await _tickets.ListUnassignedAsync(
            departmentIds, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(t => new UnassignedTicketDto(
                t.Id,
                t.TicketNumber,
                t.CustomerId,
                t.Subject,
                t.Priority.ToString(),
                t.Channel.ToString(),
                t.DepartmentId,
                t.CategoryId,
                t.CreatedAt,
                null,
                "None"))
            .ToList();

        return new PagedResult<UnassignedTicketDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
