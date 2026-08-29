namespace CRM.Application.Admin.Departments.DTOs;

public record DepartmentDto(
    Guid Id, string Name, string? NameAr, string? Description,
    Guid? BusinessHoursId, bool IsActive, DateTime CreatedAt);

public record DepartmentActiveResult(Guid Id, bool IsActive);
