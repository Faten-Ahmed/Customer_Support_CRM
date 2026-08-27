using CRM.Application.Customers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

public record PortalRegisterRequest(string FullName, string Email, string Password);
public record PortalVerifyEmailRequest(string Token);

/// <summary>
/// Handles unauthenticated customer portal registration and email verification.
/// Routes match the frontend spec: /api/v1/auth/portal/register and /api/v1/auth/portal/verify-email.
/// </summary>
[ApiController]
[Route("api/v1/auth/portal")]
public class PortalAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalAuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Registers a new customer account via the self-service portal.
    /// Sends a verification email on success.
    /// Returns 201 Created on success.
    /// Returns 409 Conflict if the email is already registered.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] PortalRegisterRequest req, CancellationToken ct)
    {
        try
        {
            var command = new RegisterCustomerCommand(req.FullName, req.Email, req.Password);
            await _mediator.Send(command, ct);
            return StatusCode(201, new { message = "Registration successful. Please check your email to verify your account." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { errors = new[] { new { code = "DUPLICATE_EMAIL", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Verifies a customer's email address using the token sent in the verification email.
    /// Returns 200 OK on success.
    /// Returns 404 Not Found if the token does not exist.
    /// Returns 422 Unprocessable Entity if the token is expired or already used.
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] PortalVerifyEmailRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new VerifyEmailCommand(req.Token), ct);
            return Ok(new { message = "Email verified successfully. You can now log in." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "TOKEN_NOT_FOUND", message = ex.Message } } });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { errors = new[] { new { code = "TOKEN_INVALID", message = ex.Message } } });
        }
    }
}
