# Deactivate & Reactivate User — Implementation Plan

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

**Story:** US-BE-065  
**Goal:** Implement `POST /api/admin/users/{id}/deactivate` and `POST /api/admin/users/{id}/reactivate`. Deactivation enforces: cannot deactivate self (`CANNOT_DEACTIVATE_SELF`); cannot deactivate the last active Admin (`CANNOT_DEACTIVATE_LAST_ADMIN`). Deactivated users' JWTs are rejected at every request via an `IsActive` check in auth middleware.

**Architecture:** `DeactivateUserCommand(TargetUserId, RequestingUserId)` → loads user, checks self and last-admin constraints, calls `user.Deactivate()`, saves. `ReactivateUserCommand(TargetUserId)` → loads user, calls `user.Reactivate()`, saves. Both return `(Id, IsActive)`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Admin/Users/Commands/DeactivateUserCommand.cs` |
| Create | `src/CRM.Application/Admin/Users/Commands/ReactivateUserCommand.cs` |
| Modify | `src/CRM.API/Controllers/AdminUsersController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/DeactivateUserCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminUsersControllerDeactivateTests.cs` |

---

## Task 1: Deactivate/Reactivate Commands

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/DeactivateUserCommandHandlerTests.cs
using CRM.Application.Admin.Users.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class DeactivateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly DeactivateUserCommandHandler _deactivateHandler;
    private readonly ReactivateUserCommandHandler _reactivateHandler;

    public DeactivateUserCommandHandlerTests()
    {
        _deactivateHandler = new DeactivateUserCommandHandler(_repo.Object);
        _reactivateHandler = new ReactivateUserCommandHandler(_repo.Object);
    }

    private User MakeActiveAdmin(Guid? id = null)
    {
        var user = User.CreateInternal(
            id ?? Guid.NewGuid(), "Admin", "User", "admin@test.com", UserRole.Admin);
        return user;
    }

    [Fact]
    public async Task Deactivate_Self_ThrowsInvalidOperationExceptionWithCannotDeactivateSelf()
    {
        var admin = MakeActiveAdmin();
        _repo.Setup(r => r.FindByIdAsync(admin.Id, default)).ReturnsAsync(admin);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateUserCommand(admin.Id, admin.Id), default));

        Assert.Contains("CANNOT_DEACTIVATE_SELF", ex.Message);
    }

    [Fact]
    public async Task Deactivate_LastActiveAdmin_ThrowsInvalidOperationException()
    {
        var caller = MakeActiveAdmin();
        var target = MakeActiveAdmin();
        _repo.Setup(r => r.FindByIdAsync(target.Id, default)).ReturnsAsync(target);
        _repo.Setup(r => r.CountActiveAdminsAsync(default)).ReturnsAsync(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateUserCommand(target.Id, caller.Id), default));

        Assert.Contains("CANNOT_DEACTIVATE_LAST_ADMIN", ex.Message);
    }

    [Fact]
    public async Task Deactivate_NonAdminUser_Succeeds()
    {
        var caller = MakeActiveAdmin();
        var agent = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "agent@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(agent.Id, default)).ReturnsAsync(agent);

        var result = await _deactivateHandler.Handle(
            new DeactivateUserCommand(agent.Id, caller.Id), default);

        Assert.False(result.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Deactivate_SecondAdmin_Succeeds()
    {
        var caller = MakeActiveAdmin();
        var target = MakeActiveAdmin();
        _repo.Setup(r => r.FindByIdAsync(target.Id, default)).ReturnsAsync(target);
        _repo.Setup(r => r.CountActiveAdminsAsync(default)).ReturnsAsync(2);

        var result = await _deactivateHandler.Handle(
            new DeactivateUserCommand(target.Id, caller.Id), default);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Reactivate_DeactivatedUser_SetsActiveTrue()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "agent@test.com", UserRole.Agent);
        user.Deactivate();
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _reactivateHandler.Handle(
            new ReactivateUserCommand(user.Id), default);

        Assert.True(result.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeactivateUserCommandHandlerTests" -v n
```

Expected: FAIL — `DeactivateUserCommand` does not exist yet.

- [ ] **Step 3: Implement DeactivateUserCommand**

```csharp
// src/CRM.Application/Admin/Users/Commands/DeactivateUserCommand.cs
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record DeactivateUserCommand(Guid TargetUserId, Guid RequestingUserId)
    : IRequest<UserActiveResult>;

public record UserActiveResult(Guid Id, bool IsActive);

public class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, UserActiveResult>
{
    private readonly IUserRepository _users;

    public DeactivateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserActiveResult> Handle(
        DeactivateUserCommand cmd, CancellationToken ct)
    {
        if (cmd.TargetUserId == cmd.RequestingUserId)
            throw new InvalidOperationException(
                "CANNOT_DEACTIVATE_SELF: An admin cannot deactivate their own account.");

        var user = await _users.FindByIdAsync(cmd.TargetUserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.TargetUserId} not found.");

        if (user.Role == UserRole.Admin)
        {
            int activeAdmins = await _users.CountActiveAdminsAsync(ct);
            if (activeAdmins <= 1)
                throw new InvalidOperationException(
                    "CANNOT_DEACTIVATE_LAST_ADMIN: At least one active Admin must remain.");
        }

        user.Deactivate();
        await _users.SaveChangesAsync(ct);

        return new UserActiveResult(user.Id, user.IsActive);
    }
}
```

- [ ] **Step 4: Implement ReactivateUserCommand**

```csharp
// src/CRM.Application/Admin/Users/Commands/ReactivateUserCommand.cs
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record ReactivateUserCommand(Guid TargetUserId) : IRequest<UserActiveResult>;

public class ReactivateUserCommandHandler
    : IRequestHandler<ReactivateUserCommand, UserActiveResult>
{
    private readonly IUserRepository _users;

    public ReactivateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserActiveResult> Handle(
        ReactivateUserCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.TargetUserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.TargetUserId} not found.");

        user.Reactivate();
        await _users.SaveChangesAsync(ct);

        return new UserActiveResult(user.Id, user.IsActive);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeactivateUserCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 6: Add endpoints to AdminUsersController**

```csharp
// Add to src/CRM.API/Controllers/AdminUsersController.cs:

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
```

- [ ] **Step 7: Write controller tests**

```csharp
// tests/CRM.API.Tests/Admin/AdminUsersControllerDeactivateTests.cs
using System.Net;
using CRM.Application.Admin.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerDeactivateTests
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
    public async Task Deactivate_Agent_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateUserCommand>(), default))
                 .ReturnsAsync(new UserActiveResult(Guid.NewGuid(), false));

        var response = await BuildClient()
            .PostAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Self_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateUserCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException(
                     "CANNOT_DEACTIVATE_SELF: Cannot deactivate own account."));

        var response = await BuildClient()
            .PostAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_DeactivatedUser_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ReactivateUserCommand>(), default))
                 .ReturnsAsync(new UserActiveResult(Guid.NewGuid(), true));

        var response = await BuildClient()
            .PostAsync($"/api/admin/users/{Guid.NewGuid()}/reactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 8: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminUsersControllerDeactivateTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Application/Admin/Users/Commands/DeactivateUserCommand.cs \
        src/CRM.Application/Admin/Users/Commands/ReactivateUserCommand.cs \
        src/CRM.API/Controllers/AdminUsersController.cs \
        tests/CRM.Application.Tests/Admin/DeactivateUserCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminUsersControllerDeactivateTests.cs
git commit -m "feat(admin): add POST /api/admin/users/{id}/deactivate with CANNOT_DEACTIVATE_SELF and CANNOT_DEACTIVATE_LAST_ADMIN guards"
```
