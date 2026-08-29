using CRM.Domain.Sla;
using CRM.Domain.Tickets.Enums;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories.Sla;

public class SlaPolicyRepository : ISlaPolicyRepository
{
    private readonly AppDbContext _db;
    public SlaPolicyRepository(AppDbContext db) => _db = db;

    public Task<SlaPolicy?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.SlaPolicies.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<SlaPolicy?> FindByDepartmentAndPriorityAsync(
        Guid departmentId, TicketPriority priority, CancellationToken ct = default)
        => _db.SlaPolicies.FirstOrDefaultAsync(
            p => p.DepartmentId == departmentId && p.Priority == priority, ct);

    public Task<SlaPolicy?> FindGlobalByPriorityAsync(
        TicketPriority priority, CancellationToken ct = default)
        => _db.SlaPolicies.FirstOrDefaultAsync(
            p => p.DepartmentId == null && p.Priority == priority, ct);

    public async Task<IReadOnlyList<SlaPolicy>> ListAllAsync(CancellationToken ct = default)
        => await _db.SlaPolicies.ToListAsync(ct);

    public async Task AddAsync(SlaPolicy policy, CancellationToken ct = default)
        => await _db.SlaPolicies.AddAsync(policy, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
