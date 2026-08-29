using CRM.Domain.Common;

namespace CRM.Domain.Users;

public record UserSummaryProjection(
    Guid Id, string FirstName, string LastName,
    string? FirstNameAr, string? LastNameAr,
    string Email, string Role,
    bool IsActive, string AvailabilityStatus, DateTimeOffset CreatedAt,
    Guid? PrimaryDepartmentId, string? PrimaryDepartmentName);

public record UserDetailProjection(
    Guid Id, string FirstName, string LastName,
    string? FirstNameAr, string? LastNameAr,
    string? JobTitle, string? JobTitleAr,
    string Email, string Role,
    bool IsActive, bool PasswordMustChange, string AvailabilityStatus, DateTimeOffset CreatedAt,
    IReadOnlyList<DepartmentAssignmentProjection> Departments,
    IReadOnlyList<SkillProjection> Skills);

public record DepartmentAssignmentProjection(Guid DepartmentId, string DepartmentName, bool IsPrimary);
public record SkillProjection(Guid CategoryId, string CategoryName);

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<User>> ListAgentsAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetDepartmentIdsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsActiveAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct = default);
    Task<int> CountActiveAdminsAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task<PagedResult<UserSummaryProjection>> ListAsync(
        UserRole? role, Guid? departmentId, bool? isActive, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task<UserDetailProjection?> GetDetailAsync(Guid userId, CancellationToken ct = default);
}
