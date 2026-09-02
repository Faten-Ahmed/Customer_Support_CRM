namespace CRM.Application.Admin.Users.DTOs;

public record UserDetailDto(
    Guid Id, string FirstName, string LastName,
    string? FirstNameAr, string? LastNameAr,
    string? JobTitle, string? JobTitleAr,
    string Email, string Role,
    bool IsActive, bool PasswordMustChange, string AvailabilityStatus,
    DateTime CreatedAt,
    IReadOnlyList<DepartmentAssignmentDto> Departments,
    IReadOnlyList<SkillDto> Skills);

public record DepartmentAssignmentDto(Guid DepartmentId, string DepartmentName, bool IsPrimary);
public record SkillDto(Guid CategoryId, string CategoryName);
