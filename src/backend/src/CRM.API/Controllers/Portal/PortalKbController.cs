using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers.Portal;

[ApiController]
[Route("api/v1/portal/kb")]
[Authorize(Roles = "Customer")]
public class PortalKbController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IKbCategoryRepository _categories;

    public PortalKbController(IMediator mediator, IKbCategoryRepository categories)
    {
        _mediator = mediator;
        _categories = categories;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories(CancellationToken ct)
    {
        var cats = await _categories.ListActiveAsync(ct);
        return Ok(cats.Select(c => new { c.Id, c.Name }));
    }

    [HttpGet("articles")]
    public async Task<IActionResult> List(
        [FromQuery] string? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        Guid? catId = !string.IsNullOrWhiteSpace(categoryId) && Guid.TryParse(categoryId, out var g)
            ? g : null;

        var filter = new KbArticleFilter(KbArticleStatus.Published, catId, null);

        var result = await _mediator.Send(
            new ListKbArticlesQuery(filter, page, pageSize, IsPortalCaller: true), ct);
        return Ok(result);
    }

    [HttpGet("articles/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var article = await _mediator.Send(
                new GetKbArticleQuery(id, IsPortalCaller: true), ct);
            return Ok(article);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Search query is required." });

        try
        {
            var results = await _mediator.Send(new SearchKbQuery(q, PortalOnly: true), ct);
            return Ok(results);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { error = ex.Errors.FirstOrDefault()?.ErrorMessage });
        }
    }
}
