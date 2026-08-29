namespace CRM.Application.Portal.DTOs;

public record PortalProfileDto(
    Guid Id,
    string FullName,
    string FullNameAr,
    string Email,
    string? Phone,
    string? CompanyName,
    string? CompanyNameAr,
    string? Country,
    string? City);
