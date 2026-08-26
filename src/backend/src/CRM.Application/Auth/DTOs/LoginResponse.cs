namespace CRM.Application.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    bool RequiresPasswordChange,
    Guid UserId,
    string FullName,
    string Role);
