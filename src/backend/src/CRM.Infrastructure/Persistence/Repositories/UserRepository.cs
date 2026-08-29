using CRM.Domain.Common;
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

    public Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(Guid userId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>(new List<Guid>());

    public async Task<bool> IsActiveAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == agentId, ct);
        return user is { IsActive: true, Role: UserRole.Agent };
    }

    public Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.AnyAsync(u => u.Email == email, ct);

    public Task<int> CountActiveAdminsAsync(CancellationToken ct = default)
        => _db.Users.CountAsync(u => u.IsActive && u.Role == UserRole.Admin, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public async Task<PagedResult<UserSummaryProjection>> ListAsync(
        UserRole? role, Guid? departmentId, bool? isActive, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Users.AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);
        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u =>
                u.FirstName.Contains(search) || u.LastName.Contains(search) ||
                u.Email.Contains(search));
        if (departmentId.HasValue)
            query = query.Where(u => u.Departments.Any(d => d.DepartmentId == departmentId.Value));

        var total = await query.CountAsync(ct);

        var rawItems = await query
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id, u.FirstName, u.LastName, u.FirstNameAr, u.LastNameAr,
                u.Email, u.Role, u.IsActive, u.AvailabilityStatus, u.CreatedAt,
                PrimaryDeptId = u.Departments
                    .Where(d => d.IsPrimary)
                    .Select(d => (Guid?)d.DepartmentId)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var primaryDeptIds = rawItems
            .Where(u => u.PrimaryDeptId.HasValue)
            .Select(u => u.PrimaryDeptId!.Value)
            .Distinct()
            .ToList();

        var deptNames = primaryDeptIds.Count > 0
            ? await _db.Departments
                .Where(d => primaryDeptIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name })
                .ToDictionaryAsync(d => d.Id, d => d.Name, ct)
            : new Dictionary<Guid, string>();

        var projections = rawItems.Select(u => new UserSummaryProjection(
            u.Id, u.FirstName, u.LastName, u.FirstNameAr, u.LastNameAr,
            u.Email, u.Role.ToString(), u.IsActive,
            u.AvailabilityStatus.ToString(), u.CreatedAt,
            u.PrimaryDeptId,
            u.PrimaryDeptId.HasValue && deptNames.TryGetValue(u.PrimaryDeptId.Value, out var n) ? n : null
        )).ToList();

        return new PagedResult<UserSummaryProjection>(projections, total, page, pageSize);
    }

    public async Task<UserDetailProjection?> GetDetailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return null;

        var deptIds = user.Departments.Select(d => d.DepartmentId).ToList();
        var deptNames = deptIds.Count > 0
            ? await _db.Departments
                .Where(d => deptIds.Contains(d.Id))
                .Select(d => new { d.Id, d.Name })
                .ToDictionaryAsync(d => d.Id, d => d.Name, ct)
            : new Dictionary<Guid, string>();

        var categoryIds = user.Skills.Select(s => s.CategoryId).ToList();
        var categoryNames = categoryIds.Count > 0
            ? await _db.TicketCategories
                .Where(c => categoryIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Name })
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct)
            : new Dictionary<Guid, string>();

        var departments = user.Departments
            .Select(d => new DepartmentAssignmentProjection(
                d.DepartmentId,
                deptNames.TryGetValue(d.DepartmentId, out var dn) ? dn : "Unknown",
                d.IsPrimary))
            .ToList();

        var skills = user.Skills
            .Select(s => new SkillProjection(
                s.CategoryId,
                categoryNames.TryGetValue(s.CategoryId, out var cn) ? cn : "Unknown"))
            .ToList();

        return new UserDetailProjection(
            user.Id, user.FirstName, user.LastName, user.FirstNameAr, user.LastNameAr,
            user.JobTitle, user.JobTitleAr, user.Email, user.Role.ToString(),
            user.IsActive, user.RequiresPasswordChange, user.AvailabilityStatus.ToString(),
            user.CreatedAt, departments, skills);
    }

    public async Task ReplaceUserDepartmentsAsync(
        Guid userId, IReadOnlyList<UserDepartment> departments, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM UserDepartments WHERE UserId = {userId}", ct);

        foreach (var d in departments)
        {
            await _db.Database.ExecuteSqlAsync(
                $"INSERT INTO UserDepartments (UserId, DepartmentId, IsPrimary) VALUES ({userId}, {d.DepartmentId}, {(d.IsPrimary ? 1 : 0)})", ct);
        }
    }

    public async Task ReplaceUserSkillsAsync(
        Guid userId, IReadOnlyList<Guid> categoryIds, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlAsync(
            $"DELETE FROM UserSkills WHERE UserId = {userId}", ct);

        foreach (var cId in categoryIds)
        {
            await _db.Database.ExecuteSqlAsync(
                $"INSERT INTO UserSkills (UserId, CategoryId) VALUES ({userId}, {cId})", ct);
        }
    }
}
