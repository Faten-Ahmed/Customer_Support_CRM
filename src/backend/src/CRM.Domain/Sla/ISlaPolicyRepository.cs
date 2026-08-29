using CRM.Domain.Tickets.Enums;

namespace CRM.Domain.Sla;

public interface ISlaPolicyRepository
{
    Task<SlaPolicy?> FindByDepartmentAndPriorityAsync(
        Guid departmentId, TicketPriority priority, CancellationToken ct = default);

    Task<SlaPolicy?> FindGlobalByPriorityAsync(
        TicketPriority priority, CancellationToken ct = default);

    Task<SlaPolicy?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SlaPolicy>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(SlaPolicy policy, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
