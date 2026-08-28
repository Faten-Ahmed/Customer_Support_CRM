namespace CRM.Application.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    bool RequiresPasswordChange,
    Guid UserId,
    string Email,
    string FullName,
    string Role);
