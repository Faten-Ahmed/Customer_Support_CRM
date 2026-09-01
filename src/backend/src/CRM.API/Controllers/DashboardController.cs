using CRM.Application.Dashboard.Queries;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
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

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(
        [FromQuery] Guid? departmentId,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetDashboardKpisQuery(CurrentUserId, CurrentUserRole, departmentId), ct);
        return Ok(new { data = result });
    }
}
