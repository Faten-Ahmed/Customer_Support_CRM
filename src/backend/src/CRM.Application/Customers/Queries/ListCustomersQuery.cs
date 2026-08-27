using CRM.Application.Common;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Queries;

public record CustomerListItemDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    bool IsVip,
    bool IsActive,
    int TicketCount,
    DateTime CreatedAt);

public record ListCustomersQuery(
    string? Search,
    bool? IsVip,
    bool? IsActive,
    int Page,
    int PageSize,
    string? SortBy,
    bool SortDesc) : IRequest<PagedResult<CustomerListItemDto>>;

public class ListCustomersQueryHandler
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerListItemDto>>
{
    private readonly ICustomerRepository _repo;

    public ListCustomersQueryHandler(ICustomerRepository repo) => _repo = repo;

    public async Task<PagedResult<CustomerListItemDto>> Handle(
        ListCustomersQuery query, CancellationToken ct)
    {
        var filter = new CustomerFilter(
            query.Search, query.IsVip, query.IsActive,
            query.Page, query.PageSize, query.SortBy, query.SortDesc);

        var paged = await _repo.ListAsync(filter, ct);

        var items = paged.Items.Select(c => new CustomerListItemDto(
            c.Id, c.FullName, c.Email, c.Phone, c.CompanyName,
            c.IsVip, c.IsActive, c.TicketCount, c.CreatedAt)).ToList();

        return new PagedResult<CustomerListItemDto>(
            items, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
