namespace CRM.Domain.Users;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(Guid userId, CancellationToken ct = default);
}
