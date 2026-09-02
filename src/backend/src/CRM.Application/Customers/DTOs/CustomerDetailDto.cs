namespace CRM.Application.Customers.DTOs;

public record ContactDto(
    Guid Id,
    string Type,
    string Value,
    bool IsPrimary);

public record CustomerDetailDto(
    Guid Id,
    string FullName,
    string FullNameAr,
    string Email,
    string? Phone,
    string? CompanyName,
    string? CompanyNameAr,
    bool IsVip,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<ContactDto> Contacts);
