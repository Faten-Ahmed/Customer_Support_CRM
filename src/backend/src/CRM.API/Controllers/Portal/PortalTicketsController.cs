using CRM.Application.Common;
using CRM.Application.Portal.Tickets.Commands;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClosePortalTicketCommand = CRM.Application.Portal.Tickets.Commands.ClosePortalTicketCommand;

namespace CRM.API.Controllers.Portal;

[ApiController]
[Route("api/v1/portal/tickets")]
[Authorize(Roles = "Customer")]
public class PortalTicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITicketRepository _tickets;
    private readonly IAttachmentRepository _attachments;
    private readonly IStorageService _storage;

    public PortalTicketsController(
        IMediator mediator,
        ITicketRepository tickets,
        IAttachmentRepository attachments,
        IStorageService storage)
    {
        _mediator = mediator;
        _tickets = tickets;
        _attachments = attachments;
        _storage = storage;
    }

    private Guid CustomerId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    public record CreatePortalTicketRequest(
        string Subject, string Description, TicketPriority Priority,
        Guid? DepartmentId, Guid? CategoryId, string? CustomFieldValues);

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _tickets.ListByCustomerAsync(CustomerId, status, null, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdDetailedAsync(id, ct);
        if (ticket is null) return NotFound(new { error = "Ticket not found." });
        if (ticket.CustomerId != CustomerId) return StatusCode(403, new { error = "Access denied." });

        return Ok(new
        {
            ticket.Id,
            ticket.TicketNumber,
            ticket.Subject,
            ticket.Description,
            Status = ticket.Status.ToString(),
            Priority = ticket.Priority.ToString(),
            Channel = ticket.Channel.ToString(),
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            AssignedAgentName = ticket.AssignedTo?.FirstName,
        });
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var ticket = await _tickets.FindByIdAsync(id, ct);
        if (ticket is null) return NotFound(new { error = "Ticket not found." });
        if (ticket.CustomerId != CustomerId) return StatusCode(403, new { error = "Access denied." });

        var result = await _mediator.Send(
            new GetTicketMessagesQuery(id, page, pageSize, IsCallerCustomer: true), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> GetAttachments(Guid id, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(id, ct);
        if (ticket is null) return NotFound(new { error = "Ticket not found." });
        if (ticket.CustomerId != CustomerId) return StatusCode(403, new { error = "Access denied." });

        var list = await _attachments.ListByTicketAsync(id, ct);

        var result = list.Select(a => new
        {
            a.Id,
            a.TicketId,
            a.FileName,
            a.ContentType,
            FileSize = a.FileSize,
            UploaderName = a.UploaderName,
            UploadedAt = a.UploadedAt,
            PresignedUrl = string.IsNullOrEmpty(a.StorageKey)
                ? null
                : _storage.GetPresignedUrlAsync(a.StorageKey, ct).GetAwaiter().GetResult(),
        });

        return Ok(result);
    }

    [HttpPost("{id:guid}/attachments")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAttachment(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        const long portalMaxBytes = 5L * 1024 * 1024;
        if (file.Length > portalMaxBytes)
            return BadRequest(new { error = "File exceeds 5 MB limit." });

        var ticket = await _tickets.FindByIdAsync(id, ct);
        if (ticket is null) return NotFound(new { error = "Ticket not found." });
        if (ticket.CustomerId != CustomerId) return StatusCode(403, new { error = "Access denied." });
        if (ticket.Status == TicketStatus.Closed)
            return BadRequest(new { error = "Cannot add attachments to a closed ticket." });

        try
        {
            var result = await _mediator.Send(new UploadAttachmentCommand(
                id, file.FileName, file.ContentType, file.Length,
                file.OpenReadStream(), CustomerId), ct);
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public record AddReplyRequest(string Body);

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddReply(
        Guid id, [FromBody] AddReplyRequest request, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(id, ct);
        if (ticket is null) return NotFound(new { error = "Ticket not found." });
        if (ticket.CustomerId != CustomerId) return StatusCode(403, new { error = "Access denied." });

        try
        {
            if (ticket.Status == TicketStatus.Resolved)
                await _mediator.Send(new ReopenTicketCommand(id, CustomerId), ct);

            var result = await _mediator.Send(new AddTicketMessageCommand(
                id, request.Body, false, null, CustomerId), ct);
            return StatusCode(201, result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> CloseTicket(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ClosePortalTicketCommand(id, CustomerId), ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePortalTicketRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateTicketPortalCommand(
                request.Subject, request.Description, request.Priority,
                request.DepartmentId, request.CategoryId, request.CustomFieldValues, CustomerId), ct);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}
