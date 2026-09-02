using CRM.Application.Agents.Commands;
using CRM.Application.Agents.Queries;
using CRM.Domain.Agents;
using CRM.Domain.Templates;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/agents/me")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class AgentMeController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    public AgentMeController(IMediator mediator) => _mediator = mediator;

    // ---- Tickets ----

    [HttpGet("tickets")]
    public async Task<IActionResult> GetMyTickets(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetMyTicketsQuery(
                CurrentUserId, status, priority, departmentId,
                page, pageSize, sortBy, sortDir), ct);
        return Ok(result);
    }

    // ---- Availability ----

    [HttpPut("availability")]
    public async Task<IActionResult> UpdateAvailability(
        [FromBody] UpdateAvailabilityRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<AvailabilityStatus>(req.Status, out var status))
            return BadRequest(new { error = $"Invalid status '{req.Status}'. Valid values: Available, Busy, Away, Offline." });

        var result = await _mediator.Send(
            new UpdateAvailabilityCommand(CurrentUserId, status), ct);

        return Ok(new { data = result });
    }

    public record UpdateAvailabilityRequest(string Status);

    // ---- Templates ----

    [HttpGet("templates")]
    public async Task<IActionResult> ListTemplates(
        [FromQuery] string? scope,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        TemplateScope? parsedScope = Enum.TryParse<TemplateScope>(scope, out var s) ? s : null;
        var result = await _mediator.Send(
            new ListMyTemplatesQuery(CurrentUserId, parsedScope, search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreatePersonalTemplateCommand(
                CurrentUserId, req.Title, req.TitleAr ?? string.Empty,
                req.Content, req.ContentAr ?? string.Empty, req.Category), ct);
        return CreatedAtAction(nameof(ListTemplates), new { }, result);
    }

    [HttpPatch("templates/{id:guid}")]
    public async Task<IActionResult> UpdateTemplate(
        Guid id, [FromBody] UpdateTemplateRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdatePersonalTemplateCommand(
                    id, CurrentUserId, req.Title, req.TitleAr,
                    req.Content, req.ContentAr, req.Category), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpDelete("templates/{id:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeletePersonalTemplateCommand(id, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpPost("templates/{id:guid}/render")]
    public async Task<IActionResult> RenderTemplate(
        Guid id, [FromBody] RenderTemplateRequest req, CancellationToken ct)
    {
        try
        {
            var rendered = await _mediator.Send(
                new RenderTemplateQuery(id, req.TicketId, CurrentUserId), ct);
            return Ok(new { data = new { rendered } });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    public record CreateTemplateRequest(string Title, string? TitleAr, string Content, string? ContentAr, string? Category);
    public record UpdateTemplateRequest(string? Title, string? TitleAr, string? Content, string? ContentAr, string? Category);
    public record RenderTemplateRequest(Guid TicketId);

    // ---- Tasks ----

    [HttpGet("tasks")]
    public async Task<IActionResult> ListTasks(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? ticketId,
        [FromQuery] bool overdue = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var parsedStatus = Enum.TryParse<AgentTaskStatus>(status, out var s) ? s : (AgentTaskStatus?)null;
        var parsedPriority = Enum.TryParse<AgentTaskPriority>(priority, out var p) ? p : (AgentTaskPriority?)null;

        var result = await _mediator.Send(
            new ListMyTasksQuery(CurrentUserId, parsedStatus, parsedPriority,
                ticketId, overdue, page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask(
        [FromBody] CreateTaskRequest req, CancellationToken ct)
    {
        var priority = Enum.TryParse<AgentTaskPriority>(req.Priority, out var p)
            ? p : AgentTaskPriority.Medium;

        var result = await _mediator.Send(
            new CreateAgentTaskCommand(
                CurrentUserId, req.Title, req.Description,
                priority, req.DueAt, req.TicketId, req.CustomerId), ct);

        return StatusCode(201, result);
    }

    [HttpPut("tasks/{id:guid}")]
    public async Task<IActionResult> UpdateTask(
        Guid id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
    {
        try
        {
            var priority = req.Priority is not null &&
                Enum.TryParse<AgentTaskPriority>(req.Priority, out var p)
                ? p : (AgentTaskPriority?)null;
            var taskStatus = req.Status is not null &&
                Enum.TryParse<AgentTaskStatus>(req.Status, out var s)
                ? s : (AgentTaskStatus?)null;

            var result = await _mediator.Send(
                new UpdateAgentTaskCommand(
                    id, CurrentUserId, req.Title, req.Description,
                    priority, taskStatus, req.DueAt), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpDelete("tasks/{id:guid}")]
    public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteAgentTaskCommand(id, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    public record CreateTaskRequest(string Title, string? Description, string? Priority,
        DateTime? DueAt, Guid? TicketId, Guid? CustomerId);
    public record UpdateTaskRequest(string? Title, string? Description, string? Priority,
        string? Status, DateTime? DueAt);
}
