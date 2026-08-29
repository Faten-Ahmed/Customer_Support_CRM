using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.Queries;
using CRM.Domain.Tickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/field-definitions")]
[Authorize(Roles = "Admin")]
public class AdminFieldDefinitionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminFieldDefinitionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
        => Ok(new { data = await _mediator.Send(
            new ListFieldDefinitionsQuery(departmentId, categoryId), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] FieldDefinitionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<FieldType>(req.FieldType, out var fieldType))
            return BadRequest(new { error = "Invalid field type." });

        try
        {
            var result = await _mediator.Send(
                new CreateFieldDefinitionCommand(
                    req.DepartmentId, req.CategoryId, req.FieldName, req.FieldNameAr,
                    fieldType, req.Options, req.IsRequired, req.SortOrder), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateFieldDefinitionRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateFieldDefinitionCommand(
                    id, req.FieldName, req.FieldNameAr, req.Options, req.IsRequired, req.SortOrder), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeactivateFieldDefinitionCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record FieldDefinitionRequest(
    Guid DepartmentId, Guid? CategoryId,
    string FieldName, string? FieldNameAr,
    string FieldType, IReadOnlyList<string>? Options,
    bool IsRequired, int SortOrder);

public record UpdateFieldDefinitionRequest(
    string? FieldName, string? FieldNameAr,
    IReadOnlyList<string>? Options, bool? IsRequired, int? SortOrder);
