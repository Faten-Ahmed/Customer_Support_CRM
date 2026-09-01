using CRM.Application.Reports.Commands;
using CRM.Application.Reports.Queries;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private UserRole CurrentUserRole
    {
        get
        {
            Enum.TryParse<UserRole>(
                User.FindFirst(ClaimTypes.Role)?.Value ?? "Agent", out var role);
            return role;
        }
    }

    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tickets")]
    public async Task<IActionResult> TicketVolume(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] Guid? departmentId,
        [FromQuery] string groupBy = "day",
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new TicketVolumeReportQuery(
                    dateFrom, dateTo, CurrentUserId, CurrentUserRole,
                    departmentId, groupBy), ct);
            return Ok(new { data = result });
        }
        catch (FluentValidation.ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpGet("sla")]
    public async Task<IActionResult> SlaCompliance(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] Guid? departmentId,
        [FromQuery] string? priority,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new SlaComplianceReportQuery(
                    dateFrom, dateTo, CurrentUserId, CurrentUserRole,
                    departmentId, priority), ct);
            return Ok(new { data = result });
        }
        catch (FluentValidation.ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpGet("agents")]
    public async Task<IActionResult> AgentPerformance(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] Guid? departmentId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new AgentPerformanceReportQuery(
                    dateFrom, dateTo, CurrentUserId, CurrentUserRole, departmentId), ct);
            return Ok(new { data = result });
        }
        catch (FluentValidation.ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpGet("csat")]
    public async Task<IActionResult> Csat(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] Guid? departmentId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new CsatReportQuery(
                    dateFrom, dateTo, CurrentUserId, CurrentUserRole, departmentId), ct);
            return Ok(new { data = result });
        }
        catch (FluentValidation.ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpGet("export")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Export(
        [FromQuery] string reportType,
        [FromQuery] string format,
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] Guid? departmentId,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _mediator.Send(
                new ExportReportCommand(
                    CurrentUserId, CurrentUserRole, reportType, format,
                    dateFrom, dateTo, departmentId), ct);

            if (result.IsAsync)
                return Accepted(new { jobId = result.JobId });

            return File(result.FileBytes!, result.ContentType!,
                result.FileName!);
        }
        catch (UnauthorizedAccessException ex)
            { return StatusCode(403, new { error = ex.Message }); }
        catch (ArgumentException ex)
            { return BadRequest(new { error = ex.Message }); }
    }
}
