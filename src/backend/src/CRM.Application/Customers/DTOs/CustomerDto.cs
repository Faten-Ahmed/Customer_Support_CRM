namespace CRM.Application.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    bool IsVip,
    bool IsActive,
    DateTime CreatedAt);
