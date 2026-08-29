using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record DeactivateDepartmentCommand(Guid DeptId) : IRequest<DepartmentActiveResult>;

public class DeactivateDepartmentCommandHandler
    : IRequestHandler<DeactivateDepartmentCommand, DepartmentActiveResult>
{
    private readonly IDepartmentRepository _depts;
    private readonly ITicketRepository _tickets;

    public DeactivateDepartmentCommandHandler(
        IDepartmentRepository depts, ITicketRepository tickets)
    {
        _depts = depts;
        _tickets = tickets;
    }

    public async Task<DepartmentActiveResult> Handle(
        DeactivateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(cmd.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {cmd.DeptId} not found.");

        int openTickets = await _tickets.CountOpenForDepartmentAsync(cmd.DeptId, ct);
        if (openTickets > 0)
            throw new InvalidOperationException(
                $"Cannot deactivate department with {openTickets} open ticket(s). Resolve or reassign them first.");

        dept.Deactivate();
        await _depts.SaveChangesAsync(ct);

        return new DepartmentActiveResult(dept.Id, dept.IsActive);
    }
}
