using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Queries;

public record GetDepartmentQuery(Guid DeptId) : IRequest<DepartmentDto>;

public class GetDepartmentQueryHandler : IRequestHandler<GetDepartmentQuery, DepartmentDto>
{
    private readonly IDepartmentRepository _depts;
    public GetDepartmentQueryHandler(IDepartmentRepository depts) => _depts = depts;
    public async Task<DepartmentDto> Handle(GetDepartmentQuery query, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(query.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {query.DeptId} not found.");
        return CreateDepartmentCommandHandler.Map(dept);
    }
}
