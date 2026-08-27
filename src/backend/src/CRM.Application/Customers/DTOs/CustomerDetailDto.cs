namespace CRM.Application.Customers.DTOs;

public record ContactDto(
    Guid Id,
    string Type,
    string Value,
    bool IsPrimary);

public record CustomerDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    bool IsVip,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<ContactDto> Contacts);
