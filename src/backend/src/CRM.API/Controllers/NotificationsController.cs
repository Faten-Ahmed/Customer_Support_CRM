using CRM.Application.Notifications.Commands;
using CRM.Application.Notifications.Queries;
using CRM.Domain.Notifications;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private UserRole CurrentUserRole
    {
        get
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "Agent";
            Enum.TryParse<UserRole>(roleClaim, out var role);
            return role;
        }
    }

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool? isRead,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool all = false,
        CancellationToken ct = default)
    {
        NotificationType? parsedType = Enum.TryParse<NotificationType>(type, out var t) ? t : null;
        var capped = Math.Min(pageSize, 50);

        var result = await _mediator.Send(
            new ListNotificationsQuery(
                CurrentUserId, CurrentUserRole,
                isRead, parsedType, page, capped, all), ct);

        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var count = await _mediator.Send(new GetUnreadCountQuery(CurrentUserId), ct);
        return Ok(new { count });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new MarkNotificationReadCommand(id, CurrentUserId), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        int markedRead = await _mediator.Send(
            new MarkAllNotificationsReadCommand(CurrentUserId), ct);
        return Ok(new { data = new { markedRead } });
    }
}
