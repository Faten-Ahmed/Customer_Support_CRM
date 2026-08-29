using CRM.Application.Sla.Commands;
using CRM.Application.Sla.Queries;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers.Admin;

[ApiController]
[Route("api/admin/sla/policies")]
[Authorize(Roles = "Admin,Manager")]
public class SlaPoliciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlaPoliciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSlaPoliciesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSlaPolicyRequest req, CancellationToken ct)
    {
        Enum.TryParse<TicketPriority>(req.Priority, out var priority);
        var id = await _mediator.Send(new CreateSlaPolicyCommand(
            priority, req.DepartmentId, req.FirstResponseMinutes, req.ResolutionMinutes,
            req.WarningThresholdPercent, req.BreachThresholdPercent,
            req.CriticalBreachThresholdPercent), ct);
        return StatusCode(201, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSlaPolicyRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateSlaPolicyCommand(
                id, req.FirstResponseMinutes, req.ResolutionMinutes,
                req.WarningThresholdPercent, req.BreachThresholdPercent,
                req.CriticalBreachThresholdPercent), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record CreateSlaPolicyRequest(
    string Priority,
    Guid? DepartmentId,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent);

public record UpdateSlaPolicyRequest(
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent);
