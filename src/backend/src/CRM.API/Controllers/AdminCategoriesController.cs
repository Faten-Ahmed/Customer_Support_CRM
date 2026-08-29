using CRM.Application.Admin.Categories.Commands;
using CRM.Application.Admin.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminCategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(new { data = await _mediator.Send(new ListCategoriesQuery(), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CategoryRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateCategoryCommand(req.Name, req.NameAr, req.ParentId, req.SortOrder), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCategoryRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateCategoryCommand(id, req.Name, req.NameAr, req.SortOrder), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeactivateCategoryCommand(id), ct);
            return Ok(new { data = new { id, isActive = false } });
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
            await _mediator.Send(new ReactivateCategoryCommand(id), ct);
            return Ok(new { data = new { id, isActive = true } });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record CategoryRequest(string Name, string? NameAr, Guid? ParentId, int SortOrder);
public record UpdateCategoryRequest(string? Name, string? NameAr, int? SortOrder);
