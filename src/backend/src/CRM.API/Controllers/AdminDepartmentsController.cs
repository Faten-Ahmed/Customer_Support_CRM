using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/departments")]
[Authorize(Roles = "Admin")]
public class AdminDepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDepartmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDepartmentsQuery(), ct);
        return Ok(new { data = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeptRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateDepartmentCommand(req.Name, req.NameAr, req.Description, req.BusinessHoursId), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("409"))
            { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateDeptRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateDepartmentCommand(id, req.Name, req.NameAr, req.Description, req.BusinessHoursId), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new DeactivateDepartmentCommand(id), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ReactivateDepartmentCommand(id), ct);
            return Ok(new { data = new { id, isActive = true } });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record CreateDeptRequest(string Name, string? NameAr, string? Description, Guid? BusinessHoursId);
public record UpdateDeptRequest(string? Name, string? NameAr, string? Description, Guid? BusinessHoursId);
