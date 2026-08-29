using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/branches")]
[Authorize(Roles = "Admin")]
public class AdminBranchesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminBranchesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(new { data = await _mediator.Send(new ListBranchesQuery(), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] BranchRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBranchCommand(req.Name, req.NameAr), ct);
        return StatusCode(201, new { data = result });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] BranchRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateBranchCommand(id, req.Name, req.NameAr), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ToggleBranchCommand(id, Activate: false), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ToggleBranchCommand(id, Activate: true), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record BranchRequest(string Name, string? NameAr);
