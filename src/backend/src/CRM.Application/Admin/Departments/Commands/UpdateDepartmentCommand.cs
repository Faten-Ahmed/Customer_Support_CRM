using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record UpdateDepartmentCommand(
    Guid DeptId, string? Name, string? NameAr, string? Description, Guid? BusinessHoursId)
    : IRequest<DepartmentDto>;

public class UpdateDepartmentCommandHandler
    : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    private readonly IDepartmentRepository _depts;

    public UpdateDepartmentCommandHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(cmd.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {cmd.DeptId} not found.");

        dept.Update(cmd.Name, cmd.NameAr, cmd.Description, cmd.BusinessHoursId);
        await _depts.SaveChangesAsync(ct);

        return CreateDepartmentCommandHandler.Map(dept);
    }
}
