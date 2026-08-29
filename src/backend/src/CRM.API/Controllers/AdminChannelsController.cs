using CRM.Application.Admin.Channels.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/channels")]
[Authorize(Roles = "Admin")]
public class AdminChannelsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminChannelsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => Ok(new { data = (await _mediator.Send(new GetChannelStatusQuery(), ct)).Channels });
}
