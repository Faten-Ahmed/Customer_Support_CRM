# Create Internal User — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-BE-063  
**Goal:** Implement `POST /api/admin/users` — creates Admin, Manager, or Agent users with a temporary password. Sets `PasswordMustChange = true`. Requires `primaryDepartmentId` for Agent/Manager roles. Returns `409` if email already exists, `422` if primaryDepartmentId missing for non-Admin roles.

**Architecture:** `CreateInternalUserCommand(FullName, Email, Password, Role, PrimaryDepartmentId?)` → checks email uniqueness, validates role-specific constraints, hashes password, creates `User` with `PasswordMustChange = true`, adds primary department assignment, saves. Returns `UserProfileDto`. Enqueues `SendWelcomeEmailJob` (fire-and-forget).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, BCrypt.Net-Next, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `src/CRM.Domain/Users/User.cs` |
| Modify | `src/CRM.Domain/Users/IUserRepository.cs` |
| Create | `src/CRM.Domain/Users/UserDepartment.cs` |
| Create | `src/CRM.Application/Admin/Users/Commands/CreateInternalUserCommand.cs` |
| Create | `src/CRM.Application/Admin/Users/DTOs/UserProfileDto.cs` |
| Create | `src/CRM.API/Controllers/AdminUsersController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/CreateInternalUserCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminUsersControllerCreateTests.cs` |

---

## Task 1: CreateInternalUser Command + Controller

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/CreateInternalUserCommandHandlerTests.cs
using CRM.Application.Admin.Users.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class CreateInternalUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly CreateInternalUserCommandHandler _handler;

    public CreateInternalUserCommandHandlerTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");
        _handler = new CreateInternalUserCommandHandler(_repo.Object, _hasher.Object, _jobs.Object);
    }

    [Fact]
    public async Task Handle_AgentWithPrimaryDept_CreatesUser()
    {
        var deptId = Guid.NewGuid();
        _repo.Setup(r => r.ExistsWithEmailAsync("agent@test.com", default)).ReturnsAsync(false);

        var result = await _handler.Handle(
            new CreateInternalUserCommand(
                "Ahmed Al-Farsi", "agent@test.com", "TempPass123!",
                UserRole.Agent, deptId),
            default);

        Assert.Equal("agent@test.com", result.Email);
        Assert.Equal("Agent", result.Role);
        _repo.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentWithoutPrimaryDept_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateInternalUserCommand(
                    "Ahmed", "agent@test.com", "Pass1!", UserRole.Agent, null),
                default));
    }

    [Fact]
    public async Task Handle_AdminWithoutPrimaryDept_Succeeds()
    {
        _repo.Setup(r => r.ExistsWithEmailAsync("admin@test.com", default)).ReturnsAsync(false);

        var result = await _handler.Handle(
            new CreateInternalUserCommand(
                "Admin User", "admin@test.com", "Pass1!", UserRole.Admin, null),
            default);

        Assert.Equal("Admin", result.Role);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _repo.Setup(r => r.ExistsWithEmailAsync("existing@test.com", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateInternalUserCommand(
                    "User", "existing@test.com", "Pass1!", UserRole.Agent, Guid.NewGuid()),
                default));

        Assert.Contains("409", ex.Message);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateInternalUserCommandHandlerTests" -v n
```

Expected: FAIL — `CreateInternalUserCommand` does not exist yet.

- [ ] **Step 3: Add fields and methods to User entity**

Add to `src/CRM.Domain/Users/User.cs` (preserving existing code):

```csharp
public string PasswordHash { get; private set; } = string.Empty;
public bool PasswordMustChange { get; private set; }
public bool IsActive { get; private set; } = true;
public DateTime CreatedAt { get; private set; }
private readonly List<UserDepartment> _departments = new();
public IReadOnlyList<UserDepartment> Departments => _departments.AsReadOnly();

public static User CreateInternal(
    Guid id, string firstName, string lastName, string email, UserRole role)
    => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Role = role,
        IsActive = true,
        AvailabilityStatus = AvailabilityStatus.Offline,
        CreatedAt = DateTime.UtcNow
    };

public void SetPassword(string passwordHash, bool mustChange = false)
{
    PasswordHash = passwordHash;
    PasswordMustChange = mustChange;
}

public void Deactivate() => IsActive = false;
public void Reactivate() => IsActive = true;
public void UpdateProfile(string fullName)
{
    var parts = fullName.Split(' ', 2);
    FirstName = parts[0];
    LastName = parts.Length > 1 ? parts[1] : string.Empty;
}

public void ReplaceDepartments(IEnumerable<UserDepartment> newDepartments)
{
    _departments.Clear();
    _departments.AddRange(newDepartments);
}
```

- [ ] **Step 4: Create UserDepartment value object**

```csharp
// src/CRM.Domain/Users/UserDepartment.cs
namespace CRM.Domain.Users;

public class UserDepartment
{
    public Guid UserId { get; init; }
    public Guid DepartmentId { get; init; }
    public bool IsPrimary { get; init; }
}
```

- [ ] **Step 5: Add to IUserRepository**

Add to `src/CRM.Domain/Users/IUserRepository.cs`:

```csharp
Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct = default);
Task<int> CountActiveAdminsAsync(CancellationToken ct = default);
Task AddAsync(User user, CancellationToken ct = default);
```

- [ ] **Step 6: Create IPasswordHasher**

```csharp
// src/CRM.Application/Common/IPasswordHasher.cs
namespace CRM.Application.Common;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
```

- [ ] **Step 7: Create UserProfileDto**

```csharp
// src/CRM.Application/Admin/Users/DTOs/UserProfileDto.cs
namespace CRM.Application.Admin.Users.DTOs;

public record UserProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    bool PasswordMustChange,
    string AvailabilityStatus,
    DateTime CreatedAt);
```

- [ ] **Step 8: Implement CreateInternalUserCommand**

```csharp
// src/CRM.Application/Admin/Users/Commands/CreateInternalUserCommand.cs
using CRM.Application.Admin.Users.DTOs;
using CRM.Application.Common;
using CRM.Domain.Users;
using Hangfire;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record CreateInternalUserCommand(
    string FullName,
    string Email,
    string Password,
    UserRole Role,
    Guid? PrimaryDepartmentId) : IRequest<UserProfileDto>;

public class CreateInternalUserCommandHandler
    : IRequestHandler<CreateInternalUserCommand, UserProfileDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IBackgroundJobClient _jobs;

    public CreateInternalUserCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IBackgroundJobClient jobs)
    {
        _users = users;
        _hasher = hasher;
        _jobs = jobs;
    }

    public async Task<UserProfileDto> Handle(
        CreateInternalUserCommand cmd, CancellationToken ct)
    {
        if (cmd.Role is UserRole.Agent or UserRole.Manager && cmd.PrimaryDepartmentId is null)
            throw new InvalidOperationException(
                "primaryDepartmentId is required for Agent and Manager roles.");

        bool emailExists = await _users.ExistsWithEmailAsync(cmd.Email, ct);
        if (emailExists)
            throw new InvalidOperationException(
                "409: A user with this email already exists.");

        var parts = cmd.FullName.Split(' ', 2);
        var user = User.CreateInternal(
            Guid.NewGuid(),
            parts[0],
            parts.Length > 1 ? parts[1] : string.Empty,
            cmd.Email,
            cmd.Role);

        user.SetPassword(_hasher.Hash(cmd.Password), mustChange: true);

        if (cmd.PrimaryDepartmentId.HasValue)
        {
            user.ReplaceDepartments(new[]
            {
                new UserDepartment
                {
                    UserId = user.Id,
                    DepartmentId = cmd.PrimaryDepartmentId.Value,
                    IsPrimary = true
                }
            });
        }

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        _jobs.Enqueue<SendWelcomeEmailJob>(
            job => job.Execute(user.Id, cmd.Email, cmd.Password, CancellationToken.None));

        return Map(user);
    }

    internal static UserProfileDto Map(User u)
        => new(u.Id, $"{u.FirstName} {u.LastName}".Trim(),
               u.Email, u.Role.ToString(), u.IsActive,
               u.PasswordMustChange, u.AvailabilityStatus.ToString(), u.CreatedAt);
}
```

- [ ] **Step 9: Create stub SendWelcomeEmailJob (placeholder for communications module)**

```csharp
// src/CRM.Application/Admin/Users/Jobs/SendWelcomeEmailJob.cs
namespace CRM.Application.Admin.Users.Jobs;

public class SendWelcomeEmailJob
{
    public Task Execute(Guid userId, string email, string tempPassword,
        CancellationToken ct = default)
    {
        // Implemented in US-BE-088 (email channel)
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 10: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateInternalUserCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 11: Create AdminUsersController**

```csharp
// src/CRM.API/Controllers/AdminUsersController.cs
using CRM.Application.Admin.Users.Commands;
using CRM.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/users")]
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
                    req.FullName, req.Email, req.Password, role,
                    req.PrimaryDepartmentId), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("409"))
            { return Conflict(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }
}

public record CreateUserRequest(
    string FullName, string Email, string Password,
    string Role, Guid? PrimaryDepartmentId);
```

- [ ] **Step 12: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminUsersControllerCreateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Users.Commands;
using CRM.Application.Admin.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerCreateTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(_mediator.Object);
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: "Admin"));
        return client;
    }

    [Fact]
    public async Task Create_ValidAgent_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateInternalUserCommand>(), default))
                 .ReturnsAsync(new UserProfileDto(
                     Guid.NewGuid(), "Ahmed Al-Farsi", "ahmed@test.com",
                     "Agent", true, true, "Offline", DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/users",
            new
            {
                fullName = "Ahmed Al-Farsi",
                email = "ahmed@test.com",
                password = "TempPass123!",
                role = "Agent",
                primaryDepartmentId = Guid.NewGuid()
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateInternalUserCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("409: Email exists."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/users",
            new { fullName = "X", email = "dup@test.com", password = "P", role = "Agent",
                  primaryDepartmentId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 13: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminUsersControllerCreateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 14: Commit**

```bash
git add src/CRM.Domain/Users/ \
        src/CRM.Application/Admin/Users/ \
        src/CRM.Application/Common/IPasswordHasher.cs \
        src/CRM.API/Controllers/AdminUsersController.cs \
        tests/CRM.Application.Tests/Admin/CreateInternalUserCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminUsersControllerCreateTests.cs
git commit -m "feat(admin): add POST /api/admin/users — create Admin/Manager/Agent with temp password and PasswordMustChange flag"
```
