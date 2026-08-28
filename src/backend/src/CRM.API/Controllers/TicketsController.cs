using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets.Enums;
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

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => Ok(); // Implemented in US-BE-021
}
