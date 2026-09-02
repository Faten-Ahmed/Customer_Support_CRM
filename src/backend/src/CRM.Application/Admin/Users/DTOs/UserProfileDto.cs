namespace CRM.Application.Admin.Users.DTOs;

public record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? FirstNameAr,
    string? LastNameAr,
    string? JobTitle,
    string? JobTitleAr,
    string Email,
    string Role,
    bool IsActive,
    bool PasswordMustChange,
    string AvailabilityStatus,
    DateTime CreatedAt);
