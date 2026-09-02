namespace CRM.Domain.Sla;

public interface IBusinessHoursRepository
{
    Task<BusinessHours?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<BusinessHours?> FindByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
    Task<BusinessHours?> FindGlobalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BusinessHours>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(BusinessHours businessHours, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
