using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record DepartmentAssignment(Guid DepartmentId, bool IsPrimary);

public record AssignUserDepartmentsCommand(
    Guid UserId,
    IReadOnlyList<DepartmentAssignment> Departments) : IRequest;

public class AssignUserDepartmentsCommandHandler
    : IRequestHandler<AssignUserDepartmentsCommand>
{
    private readonly IUserRepository _users;

    public AssignUserDepartmentsCommandHandler(IUserRepository users) => _users = users;

    public async Task Handle(AssignUserDepartmentsCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        int primaryCount = cmd.Departments.Count(d => d.IsPrimary);
        if (primaryCount != 1)
            throw new InvalidOperationException(
                "MULTIPLE_PRIMARY_DEPARTMENTS: Exactly one department must have isPrimary = true.");

        var assignments = cmd.Departments.Select(d => new UserDepartment
        {
            UserId = user.Id,
            DepartmentId = d.DepartmentId,
            IsPrimary = d.IsPrimary
        }).ToList();

        await _users.ReplaceUserDepartmentsAsync(user.Id, assignments, ct);
        await _users.SaveChangesAsync(ct);
    }
}
