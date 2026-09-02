using CRM.Application.Admin.Users.Commands;
using CRM.Application.Admin.Users.Queries;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    public AdminUsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(req.Role, out var role) ||
            role == UserRole.Customer)
            return BadRequest(new { error = "Invalid role. Use Admin, Manager, or Agent." });

        try
        {
            var result = await _mediator.Send(
                new CreateInternalUserCommand(
                    req.FirstName, req.LastName, req.Email, req.Password, role,
                    req.PrimaryDepartmentId, req.FirstNameAr, req.LastNameAr,
                    req.JobTitle, req.JobTitleAr), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("409"))
            { return Conflict(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? role,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        UserRole? parsedRole = Enum.TryParse<UserRole>(role, out var r) ? r : null;
        var result = await _mediator.Send(
            new ListUsersQuery(parsedRole, departmentId, isActive, search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new GetUserQuery(id), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateUserCommand(
                id, req.FirstName, req.LastName,
                req.FirstNameAr, req.LastNameAr, req.JobTitle, req.JobTitleAr), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new DeactivateUserCommand(id, CurrentUserId), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message.StartsWith("CANNOT_DEACTIVATE_SELF")
                ? "CANNOT_DEACTIVATE_SELF"
                : "CANNOT_DEACTIVATE_LAST_ADMIN";
            return UnprocessableEntity(new { error = ex.Message, code });
        }
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ReactivateUserCommand(id), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/departments")]
    public async Task<IActionResult> AssignDepartments(
        Guid id, [FromBody] AssignDepartmentsRequest req, CancellationToken ct)
    {
        try
        {
            var assignments = req.Departments
                .Select(d => new DepartmentAssignment(d.DepartmentId, d.IsPrimary))
                .ToArray();
            await _mediator.Send(new AssignUserDepartmentsCommand(id, assignments), ct);
            var user = await _mediator.Send(new GetUserQuery(id), ct);
            return Ok(new { data = user });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}/skills")]
    public async Task<IActionResult> AssignSkills(
        Guid id, [FromBody] AssignSkillsRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new AssignUserSkillsCommand(id, req.CategoryIds), ct);
            var user = await _mediator.Send(new GetUserQuery(id), ct);
            return Ok(new { data = user });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }
}

public record CreateUserRequest(
    string FirstName, string LastName, string Email, string Password,
    string Role, Guid? PrimaryDepartmentId,
    string? FirstNameAr = null, string? LastNameAr = null,
    string? JobTitle = null, string? JobTitleAr = null);

public record UpdateUserRequest(
    string FirstName, string LastName,
    string? FirstNameAr = null, string? LastNameAr = null,
    string? JobTitle = null, string? JobTitleAr = null);

public record AssignDepartmentsRequest(
    IReadOnlyList<DepartmentAssignmentItem> Departments);
public record DepartmentAssignmentItem(Guid DepartmentId, bool IsPrimary);
public record AssignSkillsRequest(IReadOnlyList<Guid> CategoryIds);
