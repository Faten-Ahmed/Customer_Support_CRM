using CRM.Domain.Sla;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories.Sla;

public class BusinessHoursRepository : IBusinessHoursRepository
{
    private readonly AppDbContext _db;
    public BusinessHoursRepository(AppDbContext db) => _db = db;

    public Task<BusinessHours?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.BusinessHours.Include(b => b.Holidays).FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<BusinessHours?> FindByDepartmentAsync(Guid departmentId, CancellationToken ct = default)
        => _db.BusinessHours.Include(b => b.Holidays)
            .FirstOrDefaultAsync(b => b.DepartmentId == departmentId, ct);

    public Task<BusinessHours?> FindGlobalAsync(CancellationToken ct = default)
        => _db.BusinessHours.Include(b => b.Holidays)
            .FirstOrDefaultAsync(b => b.DepartmentId == null, ct);

    public async Task<IReadOnlyList<BusinessHours>> ListAllAsync(CancellationToken ct = default)
        => await _db.BusinessHours.Include(b => b.Holidays).ToListAsync(ct);

    public async Task AddAsync(BusinessHours businessHours, CancellationToken ct = default)
        => await _db.BusinessHours.AddAsync(businessHours, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
