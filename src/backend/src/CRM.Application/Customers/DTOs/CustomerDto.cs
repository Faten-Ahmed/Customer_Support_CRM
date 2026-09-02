namespace CRM.Application.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    string FullName,
    string FullNameAr,
    string Email,
    string? Phone,
    string? CompanyName,
    string? CompanyNameAr,
    bool IsVip,
    bool IsActive,
    DateTime CreatedAt);
