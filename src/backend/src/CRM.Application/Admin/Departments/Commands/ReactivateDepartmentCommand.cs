using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record ReactivateDepartmentCommand(Guid DeptId) : IRequest;

public class ReactivateDepartmentCommandHandler : IRequestHandler<ReactivateDepartmentCommand>
{
    private readonly IDepartmentRepository _depts;

    public ReactivateDepartmentCommandHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task Handle(ReactivateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(cmd.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {cmd.DeptId} not found.");
        dept.Reactivate();
        await _depts.SaveChangesAsync(ct);
    }
}
