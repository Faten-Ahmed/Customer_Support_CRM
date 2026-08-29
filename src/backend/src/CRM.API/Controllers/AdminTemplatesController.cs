using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Admin.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/templates")]
[Authorize(Roles = "Admin")]
public class AdminTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public AdminTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new ListGlobalTemplatesQuery(search, page, pageSize), ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] GlobalTemplateRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateGlobalTemplateCommand(
                CurrentUserId, req.Title!, req.TitleAr!, req.Content!, req.ContentAr!, req.Category), ct);
        return StatusCode(201, new { data = result });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] GlobalTemplateRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateGlobalTemplateCommand(
                    id, req.Title, req.TitleAr, req.Content, req.ContentAr, req.Category), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteGlobalTemplateCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }
}

public record GlobalTemplateRequest(
    string? Title, string? TitleAr,
    string? Content, string? ContentAr,
    string? Category);
