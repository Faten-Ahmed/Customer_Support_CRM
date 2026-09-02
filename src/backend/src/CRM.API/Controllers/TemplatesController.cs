using CRM.Application.Admin.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListGlobalTemplatesQuery(search, page, pageSize), ct);
        return Ok(new
        {
            data = result.Items,
            meta = new { result.Page, result.PageSize, result.TotalCount, result.TotalPages }
        });
    }
}
