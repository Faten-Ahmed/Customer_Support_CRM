namespace CRM.Domain.Users;

public class UserDepartment
{
    public Guid UserId { get; init; }
    public Guid DepartmentId { get; init; }
    public bool IsPrimary { get; init; }
}
