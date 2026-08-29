namespace CRM.Application.Admin.Users.DTOs;

public record UserSummaryDto(
    Guid Id, string FirstName, string LastName,
    string? FirstNameAr, string? LastNameAr,
    string Email, string Role,
    bool IsActive, string AvailabilityStatus, DateTime CreatedAt,
    Guid? PrimaryDepartmentId, string? PrimaryDepartmentName);
