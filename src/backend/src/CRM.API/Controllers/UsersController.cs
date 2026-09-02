using CRM.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _users;

    public UsersController(IUserRepository users) => _users = users;

    [HttpGet("agents")]
    public async Task<IActionResult> ListAgents(CancellationToken ct)
    {
        var agents = await _users.ListAgentsAsync(ct);
        var result = agents.Select(u => new
        {
            u.Id,
            Name = $"{u.FirstName} {u.LastName}",
            u.Role,
            u.Email,
        });
        return Ok(result);
    }
}
