using CRM.Application.Customers.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Customers.Queries;

public record GetCustomerTicketsQuery(
    Guid CustomerId,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    string? Status,
    int Page,
    int PageSize) : IRequest<PagedResult<CustomerTicketDto>>;

public class GetCustomerTicketsQueryHandler
    : IRequestHandler<GetCustomerTicketsQuery, PagedResult<CustomerTicketDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public GetCustomerTicketsQueryHandler(
        ICustomerRepository customers,
        ITicketRepository tickets,
        IUserRepository users)
    {
        _customers = customers;
        _tickets = tickets;
        _users = users;
    }

    public async Task<PagedResult<CustomerTicketDto>> Handle(
        GetCustomerTicketsQuery query, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(query.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {query.CustomerId} not found.");

        IReadOnlyList<Guid>? departmentIds = null;
        if (query.RequestingUserRole == UserRole.Agent)
        {
            departmentIds = await _users.GetDepartmentIdsAsync(query.RequestingUserId, ct);
        }

        var paged = await _tickets.ListByCustomerAsync(
            query.CustomerId, query.Status, departmentIds,
            query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(t => new CustomerTicketDto(
                t.TicketNumber, t.Subject, t.Status, t.Priority, t.CreatedAt, t.Category))
            .ToList();

        return new PagedResult<CustomerTicketDto>(
            dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
