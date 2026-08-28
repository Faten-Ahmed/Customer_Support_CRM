namespace CRM.Application.Portal.DTOs;

public record PortalProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? CompanyName,
    string? Country,
    string? City);
