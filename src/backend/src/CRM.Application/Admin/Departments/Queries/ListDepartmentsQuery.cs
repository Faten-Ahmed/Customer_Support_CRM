using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Queries;

public record ListDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

public class ListDepartmentsQueryHandler
    : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _depts;

    public ListDepartmentsQueryHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        ListDepartmentsQuery query, CancellationToken ct)
    {
        var depts = await _depts.ListAsync(ct);
        return depts.Select(CreateDepartmentCommandHandler.Map).ToList();
    }
}
