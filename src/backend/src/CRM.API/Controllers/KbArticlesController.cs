using CRM.Application.KnowledgeBase.Commands;
using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.KnowledgeBase;
using CRM.Domain.Users;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/kb/articles")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class KbArticlesController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    public KbArticlesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateKbArticleRequest req, CancellationToken ct)
    {
        Enum.TryParse<KbVisibility>(req.Visibility ?? "Internal", out var visibility);
        try
        {
            var result = await _mediator.Send(new CreateKbArticleCommand(
                req.Title, req.CategoryId, CurrentUserId,
                visibility, req.Content, req.TitleAr, req.ContentAr), ct);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateKbArticleRequest req, CancellationToken ct)
    {
        Enum.TryParse<KbVisibility>(req.Visibility ?? "Internal", out var visibility);
        try
        {
            var result = await _mediator.Send(new UpdateKbArticleCommand(
                id, req.Title, req.CategoryId, visibility,
                req.Content, req.TitleAr, req.ContentAr), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetKbArticleQuery(id, false), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        KbArticleStatus? parsedStatus = Enum.TryParse<KbArticleStatus>(status, out var s) ? s : null;
        var filter = new KbArticleFilter(parsedStatus, categoryId, null);

        var result = await _mediator.Send(new ListKbArticlesQuery(filter, page, pageSize, false), ct);
        return Ok(result);
    }

    [HttpGet("/api/v1/kb/search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q, CancellationToken ct)
    {
        try
        {
            var results = await _mediator.Send(new SearchKbQuery(q ?? string.Empty, false), ct);
            return Ok(results);
        }
        catch (ValidationException ex)
        {
            return UnprocessableEntity(new { errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
        }
    }

    [HttpPost("{id:guid}/submit-review")]
    public async Task<IActionResult> SubmitForReview(Guid id, CancellationToken ct)
    {
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Agent";
        Enum.TryParse<UserRole>(roleClaim, out var role);

        try
        {
            await _mediator.Send(
                new SubmitKbArticleForReviewCommand(id, CurrentUserId, role), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ApproveKbArticleCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message, code = "INVALID_STATUS_TRANSITION" }); }
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectKbArticleRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new RejectKbArticleCommand(id, req.RejectionNote), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ArchiveKbArticleCommand(id), ct);
            return Ok();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Agent";
        Enum.TryParse<UserRole>(roleClaim, out var role);

        try
        {
            await _mediator.Send(new DeleteKbArticleCommand(id, CurrentUserId, role), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message, code = "MUST_ARCHIVE_FIRST" });
        }
    }
}

public record CreateKbArticleRequest(
    string Title,
    Guid CategoryId,
    string? Content,
    string? TitleAr,
    string? ContentAr,
    string? Visibility);

public record UpdateKbArticleRequest(
    string Title,
    Guid CategoryId,
    string? Content,
    string? TitleAr,
    string? ContentAr,
    string? Visibility);

public record RejectKbArticleRequest(string RejectionNote);
