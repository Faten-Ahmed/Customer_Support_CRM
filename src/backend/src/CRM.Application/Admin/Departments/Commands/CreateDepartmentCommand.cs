using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record CreateDepartmentCommand(
    string Name, string? NameAr, string? Description, Guid? BusinessHoursId)
    : IRequest<DepartmentDto>;

public class CreateDepartmentCommandHandler
    : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IDepartmentRepository _depts;

    public CreateDepartmentCommandHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task<DepartmentDto> Handle(
        CreateDepartmentCommand cmd, CancellationToken ct)
    {
        bool exists = await _depts.ExistsByNameAsync(cmd.Name, ct);
        if (exists)
            throw new InvalidOperationException(
                $"409: A department named '{cmd.Name}' already exists.");

        var dept = Department.Create(cmd.Name, cmd.NameAr, cmd.Description, cmd.BusinessHoursId);
        await _depts.AddAsync(dept, ct);
        await _depts.SaveChangesAsync(ct);

        return Map(dept);
    }

    internal static DepartmentDto Map(Department d)
        => new(d.Id, d.Name, d.NameAr, d.Description, d.BusinessHoursId, d.IsActive, d.CreatedAt);
}
