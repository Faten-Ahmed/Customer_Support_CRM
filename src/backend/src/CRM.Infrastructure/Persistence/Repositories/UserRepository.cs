using CRM.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<List<User>> ListAgentsAsync(CancellationToken ct = default)
        => _db.Users
            .Where(u => u.IsActive && (u.Role == UserRole.Agent || u.Role == UserRole.Manager || u.Role == UserRole.Admin))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    // Stub until agent-department assignment domain is implemented (US-BE-063+)
    public Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>(new List<Guid>());

    public async Task<bool> IsActiveAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == agentId, ct);
        return user is { IsActive: true, Role: UserRole.Agent };
    }
}
