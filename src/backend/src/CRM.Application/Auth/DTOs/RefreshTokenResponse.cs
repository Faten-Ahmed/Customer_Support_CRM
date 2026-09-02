namespace CRM.Application.Auth.DTOs;

public record RefreshTokenResponse(string AccessToken, string NewRefreshToken);
