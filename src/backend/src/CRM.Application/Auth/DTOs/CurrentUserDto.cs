namespace CRM.Application.Auth.DTOs;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string FirstNameAr,
    string LastName,
    string LastNameAr,
    string? JobTitle,
    string? JobTitleAr,
    string Role,
    bool IsActive,
    bool RequiresPasswordChange,
    string? AvatarUrl,
    string? DepartmentName);
