using CRM.Application.Sla.Commands;
using CRM.Application.Sla.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers.Admin;

[ApiController]
[Route("api/admin/business-hours")]
[Authorize(Roles = "Admin")]
public class BusinessHoursController : ControllerBase
{
    private readonly IMediator _mediator;
    public BusinessHoursController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _mediator.Send(new GetBusinessHoursQuery(), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateBusinessHoursRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateBusinessHoursCommand(
                id, req.WorkDays,
                TimeOnly.Parse(req.StartTime),
                TimeOnly.Parse(req.EndTime),
                req.TimeZone), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/holidays")]
    public async Task<IActionResult> AddHoliday(
        Guid id, [FromBody] AddHolidayRequest req, CancellationToken ct)
    {
        try
        {
            var holidayId = await _mediator.Send(new AddHolidayCommand(
                id, DateOnly.Parse(req.Date), req.Name), ct);
            return StatusCode(201, new { id = holidayId });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}/holidays/{holidayId:guid}")]
    public async Task<IActionResult> DeleteHoliday(
        Guid id, Guid holidayId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteHolidayCommand(id, holidayId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record UpdateBusinessHoursRequest(
    string[] WorkDays, string StartTime, string EndTime, string TimeZone);

public record AddHolidayRequest(string Date, string Name);
