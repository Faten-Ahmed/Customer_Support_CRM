using CRM.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    public sealed class LoginInternalRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login-internal")]
    public async Task<IActionResult> LoginInternal(
        [FromBody] LoginInternalRequest request, CancellationToken ct)
    {
        try
        {
            var command = new LoginInternalCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, ct);

            Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                result.AccessToken,
                result.RequiresPasswordChange,
                result.UserId,
                result.FullName,
                result.Role
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
