using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FluentValidation;

namespace CRM.API.Controllers.Portal;

[ApiController]
[Route("api/v1/portal")]
[Authorize(Roles = "Customer")]
public class PortalController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortalController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentCustomerId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetMyPortalProfileQuery(CurrentCustomerId), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdatePortalProfileCommand(CurrentCustomerId, req.FullName, req.FullNameAr, req.Phone, req.City), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> PatchProfile(
        [FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdatePortalProfileCommand(CurrentCustomerId, req.FullName, req.FullNameAr, req.Phone, req.City), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("surveys/{id:guid}")]
    public async Task<IActionResult> GetSurvey(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetPortalSurveyQuery(id, CurrentCustomerId), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpPost("surveys/{id:guid}/submit")]
    public async Task<IActionResult> SubmitSurvey(
        Guid id, [FromBody] SurveySubmitRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(
                new SubmitPortalSurveyCommand(id, CurrentCustomerId, req.Rating, req.Comment), ct);
            return Ok(new { message = "Survey submitted successfully." });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Errors.First().ErrorCode }); }
    }
}

public record UpdateProfileRequest(string? FullName, string? FullNameAr, string? Phone, string? City);
public record SurveySubmitRequest(int Rating, string? Comment);
