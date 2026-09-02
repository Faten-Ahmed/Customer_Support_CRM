using CRM.Application.Chat.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/chat")]
[Authorize(Roles = "Agent,Manager,Admin")]
public class ChatSessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatSessionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("sessions/waiting")]
    public async Task<IActionResult> GetWaiting(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWaitingSessionsQuery(), ct);
        return Ok(result);
    }
}
