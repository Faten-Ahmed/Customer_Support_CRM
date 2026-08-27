using CRM.Application.Auth.Commands;
using CRM.Application.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    public sealed class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var command = new LoginInternalCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, ct);

            Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            if (result.RequiresPasswordChange)
                return StatusCode(423, new { code = "PASSWORD_CHANGE_REQUIRED" });

            return Ok(new
            {
                result.AccessToken,
                user = new
                {
                    id = result.UserId,
                    email = result.Email,
                    role = result.Role,
                    passwordMustChange = result.RequiresPasswordChange,
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { code = "INVALID_CREDENTIALS" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return Ok(new { message = "If the email exists, a reset link has been sent." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var rawToken = Request.Cookies["refreshToken"];

        if (!string.IsNullOrEmpty(rawToken))
            await _mediator.Send(new LogoutCommand(rawToken), ct);

        Response.Cookies.Delete("refreshToken");
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            var result = await _mediator.Send(new GetCurrentUserQuery(userId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    public record ChangeFirstLoginPasswordRequest(string CurrentPassword, string NewPassword);

    [Authorize]
    [HttpPost("change-password-first-login")]
    public async Task<IActionResult> ChangeFirstLoginPassword(
        [FromBody] ChangeFirstLoginPasswordRequest request,
        CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        try
        {
            await _mediator.Send(
                new ChangeFirstLoginPasswordCommand(userId, request.CurrentPassword, request.NewPassword),
                ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(command, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public sealed class PortalRegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    [HttpPost("portal/register")]
    public IActionResult PortalRegister([FromBody] PortalRegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { code = "PASSWORD_MISMATCH" });

        // TODO: implement portal registration (US-BE-009)
        return StatusCode(501, new { code = "NOT_IMPLEMENTED", message = "Portal registration is not yet implemented." });
    }

    [HttpPost("portal/resend-verification")]
    public IActionResult PortalResendVerification([FromBody] ResendVerificationRequest request)
    {
        // TODO: implement email verification resend (US-BE-009)
        return StatusCode(501, new { code = "NOT_IMPLEMENTED", message = "Email verification resend is not yet implemented." });
    }

    public sealed class ResendVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var rawToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(new { code = "REFRESH_TOKEN_MISSING" });

        try
        {
            var result = await _mediator.Send(new RefreshTokenCommand(rawToken), ct);

            Response.Cookies.Append("refreshToken", result.NewRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new { accessToken = result.AccessToken });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { code = "INVALID_REFRESH_TOKEN" });
        }
    }
}
