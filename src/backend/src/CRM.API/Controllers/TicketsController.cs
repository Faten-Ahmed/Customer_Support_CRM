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
}
