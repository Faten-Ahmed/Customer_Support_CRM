using CRM.Application.Portal.Tickets.Commands;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers.Portal;

[ApiController]
[Route("api/v1/portal/tickets")]
[Authorize(Roles = "Customer")]
public class PortalTicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PortalTicketsController(IMediator mediator) => _mediator = mediator;

    private Guid CustomerId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    public record CreatePortalTicketRequest(
        string Subject, string Description, TicketPriority Priority,
        Guid? CategoryId, string? CustomFieldValues);

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePortalTicketRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateTicketPortalCommand(
                request.Subject, request.Description, request.Priority,
                request.CategoryId, request.CustomFieldValues, CustomerId), ct);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => Ok(); // Stub — implemented in portal list story
}
