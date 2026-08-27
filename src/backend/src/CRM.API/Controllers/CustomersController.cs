using CRM.Application.Customers.Commands;
using CRM.Application.Customers.Queries;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

public record CreateCustomerRequest(string FullName, string Email, string? Phone, string? CompanyName);
public record UpdateCustomerRequest(string FullName, string? Phone, string? CompanyName);
public record SetVipRequest(bool IsVip);
public record AddContactRequest(string Type, string Value, bool IsPrimary);

[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Creates a new customer (Admin/Manager/Agent only).
    /// Returns 201 Created with the new customer's resource location.
    /// Returns 409 if a customer with the same email already exists.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req, CancellationToken ct)
    {
        try
        {
            var command = new CreateCustomerCommand(req.FullName, req.Email, req.Phone, req.CompanyName);
            var dto = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { errors = new[] { new { code = "DUPLICATE_EMAIL", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Gets a single customer by ID.
    /// Returns 200 with CustomerDetailDto.
    /// Returns 404 if not found.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var dto = await _mediator.Send(new GetCustomerQuery(id), ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "CUSTOMER_NOT_FOUND", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Lists customers with optional filtering, sorting, and pagination.
    /// Returns 200 with a paged result.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isVip,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDesc = false,
        CancellationToken ct = default)
    {
        var query = new ListCustomersQuery(search, isVip, isActive, page, pageSize, sortBy, sortDesc);
        var result = await _mediator.Send(query, ct);

        return Ok(new
        {
            items = result.Items,
            meta = new
            {
                page = result.Page,
                pageSize = result.PageSize,
                totalCount = result.TotalCount,
                totalPages = result.TotalPages,
            }
        });
    }

    /// <summary>
    /// Updates customer's profile fields (FullName, Phone, CompanyName).
    /// Returns 200 with the updated CustomerDetailDto.
    /// Returns 404 if not found.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest req, CancellationToken ct)
    {
        try
        {
            var command = new UpdateCustomerCommand(id, req.FullName, req.Phone, req.CompanyName);
            var dto = await _mediator.Send(command, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "CUSTOMER_NOT_FOUND", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Deletes a customer (Admin only).
    /// Returns 204 No Content.
    /// Returns 404 if not found.
    /// Returns 422 if the customer has open tickets.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteCustomerCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "CUSTOMER_NOT_FOUND", message = ex.Message } } });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { errors = new[] { new { code = "OPEN_TICKET_EXISTS", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Sets or clears the VIP flag on a customer (Admin/Manager only).
    /// Returns 204 No Content.
    /// Returns 404 if not found.
    /// </summary>
    [HttpPatch("{id:guid}/vip")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> SetVip(Guid id, [FromBody] SetVipRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new SetCustomerVipCommand(id, req.IsVip), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "CUSTOMER_NOT_FOUND", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Adds a contact method (email/phone/etc.) to a customer.
    /// Returns 201 Created with the new ContactDto.
    /// Returns 400 if the contact type is invalid.
    /// Returns 404 if the customer is not found.
    /// </summary>
    [HttpPost("{id:guid}/contacts")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] AddContactRequest req, CancellationToken ct)
    {
        try
        {
            var command = new AddCustomerContactCommand(id, req.Type, req.Value, req.IsPrimary);
            var dto = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id }, dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { errors = new[] { new { code = "INVALID_CONTACT_TYPE", message = ex.Message } } });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "CUSTOMER_NOT_FOUND", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Removes a contact method from a customer.
    /// Returns 204 No Content.
    /// Returns 404 if the customer or contact is not found.
    /// Returns 422 if the contact is the sole primary contact.
    /// </summary>
    [HttpDelete("{id:guid}/contacts/{contactId:guid}")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> RemoveContact(Guid id, Guid contactId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new RemoveCustomerContactCommand(id, contactId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "NOT_FOUND", message = ex.Message } } });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { errors = new[] { new { code = "SOLE_PRIMARY_CONTACT", message = ex.Message } } });
        }
    }

    /// <summary>
    /// Returns paginated ticket history for a specific customer.
    /// Agents see only tickets from their own departments.
    /// Returns 200 with paged result.
    /// Returns 404 if customer not found.
    /// </summary>
    [HttpGet("{id:guid}/tickets")]
    [Authorize(Roles = "Admin,Manager,Agent")]
    public async Task<IActionResult> GetTickets(
        Guid id,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdClaim, out var requestingUserId))
            return Unauthorized();

        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value ?? string.Empty;

        var requestingRole = roleClaim switch
        {
            "Admin" => UserRole.Admin,
            "Manager" => UserRole.Manager,
            "Agent" => UserRole.Agent,
            _ => UserRole.Agent
        };

        try
        {
            var result = await _mediator.Send(
                new GetCustomerTicketsQuery(id, requestingUserId, requestingRole, status, page, pageSize), ct);

            return Ok(new
            {
                items = result.Items,
                meta = new
                {
                    page = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { errors = new[] { new { code = "CUSTOMER_NOT_FOUND", message = ex.Message } } });
        }
    }
}
