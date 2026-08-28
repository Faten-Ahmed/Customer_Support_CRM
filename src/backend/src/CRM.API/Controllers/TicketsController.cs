using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TicketsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    public record CreateTicketRequest(
        Guid CustomerId,
        string Subject,
        string Description,
        TicketPriority Priority,
        TicketChannel Channel,
        Guid? DepartmentId,
        Guid? CategoryId,
        string? CustomFieldValues);

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateTicketInternalCommand(
                request.CustomerId, request.Subject, request.Description,
                request.Priority, request.Channel, CurrentUserId,
                request.DepartmentId, request.CategoryId, request.CustomFieldValues), ct);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] TicketStatus? status,
        [FromQuery] TicketPriority? priority,
        [FromQuery] Guid? customerId,
        [FromQuery] Guid? assignedToUserId,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
        Enum.TryParse<UserRole>(roleClaim, out var role);

        var result = await _mediator.Send(new ListTicketsQuery(
            status, priority, customerId, assignedToUserId, categoryId,
            page, pageSize, sortBy, sortDesc, CurrentUserId, role, search), ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetTicketQuery(id), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    public record UpdateTicketRequest(
        string Subject, string Description, TicketPriority Priority,
        Guid? CategoryId, Guid? DepartmentId, string? CustomFieldValues);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateTicketRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateTicketCommand(
                id, request.Subject, request.Description, request.Priority,
                request.CategoryId, request.DepartmentId, request.CustomFieldValues,
                CurrentUserId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    public record AssignTicketRequest(Guid AgentId);

    [Authorize(Roles = "Admin,Manager")]
    [HttpPatch("{id:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid id, [FromBody] AssignTicketRequest request, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AssignTicketCommand(id, request.AgentId, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public record ChangeStatusRequest(TicketStatus Status);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(
                new ChangeTicketStatusCommand(id, request.Status, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public record TransferTicketRequest(
        Guid? TargetDepartmentId, Guid? TargetAgentId, string Reason);

    [Authorize(Roles = "Admin,Manager")]
    [HttpPatch("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(
        Guid id, [FromBody] TransferTicketRequest request, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new TransferTicketCommand(
                id, request.TargetDepartmentId, request.TargetAgentId,
                request.Reason, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    public record EscalateTicketRequest(string Reason);

    [HttpPatch("{id:guid}/escalate")]
    public async Task<IActionResult> Escalate(
        Guid id, [FromBody] EscalateTicketRequest request, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new EscalateTicketCommand(id, request.Reason, CurrentUserId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetTicketMessagesQuery(id, page, pageSize, IsCallerCustomer: false), ct);
        return Ok(result);
    }

    public record AddMessageRequest(string Body, bool IsInternal);

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(
        Guid id, [FromBody] AddMessageRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new AddTicketMessageCommand(
                id, request.Body, request.IsInternal, CurrentUserId, null), ct);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            var result = await _mediator.Send(new UploadAttachmentCommand(
                id, file.FileName, file.ContentType, file.Length,
                file.OpenReadStream(), CurrentUserId), ct);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTicketHistoryQuery(id, page, pageSize), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(
        Guid id, Guid attachmentId, CancellationToken ct)
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
        Enum.TryParse<UserRole>(roleClaim, out var role);

        try
        {
            await _mediator.Send(
                new DeleteAttachmentCommand(id, attachmentId, CurrentUserId, role), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }
}
