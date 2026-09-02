using CRM.Application.Admin.Users.DTOs;
using CRM.Domain.Common;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Queries;

public record ListUsersQuery(
    UserRole? Role, Guid? DepartmentId, bool? IsActive,
    string? Search, int Page, int PageSize)
    : IRequest<PagedResult<UserSummaryDto>>;

public class ListUsersQueryHandler
    : IRequestHandler<ListUsersQuery, PagedResult<UserSummaryDto>>
{
    private readonly IUserRepository _users;

    public ListUsersQueryHandler(IUserRepository users) => _users = users;

    public async Task<PagedResult<UserSummaryDto>> Handle(
        ListUsersQuery query, CancellationToken ct)
    {
        var paged = await _users.ListAsync(
            query.Role, query.DepartmentId, query.IsActive,
            query.Search, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(p => new UserSummaryDto(
                p.Id, p.FirstName, p.LastName, p.FirstNameAr, p.LastNameAr,
                p.Email, p.Role, p.IsActive,
                p.AvailabilityStatus, p.CreatedAt.DateTime,
                p.PrimaryDepartmentId, p.PrimaryDepartmentName))
            .ToList();

        return new PagedResult<UserSummaryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
