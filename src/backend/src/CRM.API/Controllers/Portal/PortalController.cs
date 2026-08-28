using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
                new UpdatePortalProfileCommand(CurrentCustomerId, req.FullName, req.Phone, req.City), ct);
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
                new UpdatePortalProfileCommand(CurrentCustomerId, req.FullName, req.Phone, req.City), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record UpdateProfileRequest(string? FullName, string? Phone, string? City);
