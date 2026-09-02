# User Department & Skill Assignments — Implementation Plan

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

**Story:** US-BE-066  
**Goal:** Implement `PUT /api/admin/users/{id}/departments` (atomic replace of all department assignments — exactly one `isPrimary = true`) and `PUT /api/admin/users/{id}/skills` (atomic replace of skill/category assignments — empty list removes all skills; unknown category IDs return 422).

> **⚠️ Implementation divergences from original plan:**
> - `AssignUserSkillsCommandHandler` does **not** call `user.ReplaceSkills()` + `SaveChanges`. Instead it calls `IUserRepository.ReplaceUserSkillsAsync(userId, categoryIds)` which issues raw SQL (`DELETE … WHERE UserId` then bulk `INSERT`). This bypasses EF Core change tracking, which did not reliably persist changes to the `_skills` private backing field.
> - `IUserRepository` has an additional method: `Task ReplaceUserSkillsAsync(Guid userId, IReadOnlyList<Guid> categoryIds, CancellationToken ct = default)`
> - Frontend: the skills section in the user-edit dialog is shown **only** for Agent and Manager roles — Admin users do not have skills.

**Architecture:** `AssignUserDepartmentsCommand(UserId, Departments[])` → validates exactly one primary, replaces assignments atomically. `AssignUserSkillsCommand(UserId, CategoryIds[])` → validates all category IDs exist, replaces atomically. Both commands return updated `UserDetailDto`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Admin/Users/Commands/AssignUserDepartmentsCommand.cs` |
| Create | `src/CRM.Application/Admin/Users/Commands/AssignUserSkillsCommand.cs` |
| Create | `src/CRM.Domain/Users/UserSkill.cs` |
| Modify | `src/CRM.Domain/Users/User.cs` |
| Modify | `src/CRM.Domain/Users/IUserRepository.cs` |
| Modify | `src/CRM.API/Controllers/AdminUsersController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/AssignUserCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminUsersControllerAssignTests.cs` |

---

## Task 1: Department & Skill Assignment Commands

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/AssignUserCommandHandlerTests.cs
using CRM.Application.Admin.Users.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class AssignUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly Mock<ICategoryExistenceChecker> _cats = new();
    private readonly AssignUserDepartmentsCommandHandler _deptHandler;
    private readonly AssignUserSkillsCommandHandler _skillHandler;

    public AssignUserCommandHandlerTests()
    {
        _deptHandler = new AssignUserDepartmentsCommandHandler(_repo.Object);
        _skillHandler = new AssignUserSkillsCommandHandler(_repo.Object, _cats.Object);
    }

    [Fact]
    public async Task AssignDepartments_ExactlyOnePrimary_Succeeds()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var depts = new[]
        {
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: true),
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: false)
        };

        await _deptHandler.Handle(
            new AssignUserDepartmentsCommand(user.Id, depts), default);

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Equal(2, user.Departments.Count);
        Assert.Single(user.Departments, d => d.IsPrimary);
    }

    [Fact]
    public async Task AssignDepartments_MultiplePrimary_ThrowsInvalidOperationException()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var depts = new[]
        {
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: true),
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: true)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deptHandler.Handle(
                new AssignUserDepartmentsCommand(user.Id, depts), default));

        Assert.Contains("MULTIPLE_PRIMARY_DEPARTMENTS", ex.Message);
    }

    [Fact]
    public async Task AssignDepartments_NoPrimary_ThrowsInvalidOperationException()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var depts = new[]
        {
            new DepartmentAssignment(Guid.NewGuid(), IsPrimary: false)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deptHandler.Handle(
                new AssignUserDepartmentsCommand(user.Id, depts), default));

        Assert.Contains("primary", ex.Message.ToLower());
    }

    [Fact]
    public async Task AssignSkills_ValidCategoryIds_Succeeds()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);
        var catId = Guid.NewGuid();
        _cats.Setup(c => c.AllExistAsync(new[] { catId }, default)).ReturnsAsync(true);

        await _skillHandler.Handle(
            new AssignUserSkillsCommand(user.Id, new[] { catId }), default);

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Single(user.Skills);
    }

    [Fact]
    public async Task AssignSkills_UnknownCategoryId_ThrowsInvalidOperationException()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);
        var catId = Guid.NewGuid();
        _cats.Setup(c => c.AllExistAsync(new[] { catId }, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _skillHandler.Handle(
                new AssignUserSkillsCommand(user.Id, new[] { catId }), default));
    }

    [Fact]
    public async Task AssignSkills_EmptyList_ClearsAllSkills()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Al-Farsi", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        await _skillHandler.Handle(
            new AssignUserSkillsCommand(user.Id, Array.Empty<Guid>()), default);

        Assert.Empty(user.Skills);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AssignUserCommandHandlerTests" -v n
```

Expected: FAIL — commands do not exist yet.

- [ ] **Step 3: Create UserSkill value object**

```csharp
// src/CRM.Domain/Users/UserSkill.cs
namespace CRM.Domain.Users;

public class UserSkill
{
    public Guid UserId { get; init; }
    public Guid CategoryId { get; init; }
}
```

- [ ] **Step 4: Add Skills to User entity**

Add to `src/CRM.Domain/Users/User.cs`:

```csharp
private readonly List<UserSkill> _skills = new();
public IReadOnlyList<UserSkill> Skills => _skills.AsReadOnly();

public void ReplaceSkills(IEnumerable<UserSkill> newSkills)
{
    _skills.Clear();
    _skills.AddRange(newSkills);
}
```

- [ ] **Step 5: Create ICategoryExistenceChecker**

```csharp
// src/CRM.Application/Admin/Users/Commands/ICategoryExistenceChecker.cs
namespace CRM.Application.Admin.Users.Commands;

public interface ICategoryExistenceChecker
{
    Task<bool> AllExistAsync(IEnumerable<Guid> categoryIds, CancellationToken ct = default);
}
```

- [ ] **Step 6: Implement AssignUserDepartmentsCommand**

```csharp
// src/CRM.Application/Admin/Users/Commands/AssignUserDepartmentsCommand.cs
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record DepartmentAssignment(Guid DepartmentId, bool IsPrimary);

public record AssignUserDepartmentsCommand(
    Guid UserId,
    IReadOnlyList<DepartmentAssignment> Departments) : IRequest;

public class AssignUserDepartmentsCommandHandler
    : IRequestHandler<AssignUserDepartmentsCommand>
{
    private readonly IUserRepository _users;

    public AssignUserDepartmentsCommandHandler(IUserRepository users) => _users = users;

    public async Task Handle(AssignUserDepartmentsCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        if (cmd.Departments.Count(d => d.IsPrimary) != 1)
            throw new InvalidOperationException(
                "MULTIPLE_PRIMARY_DEPARTMENTS: Exactly one department must have isPrimary = true.");

        var assignments = cmd.Departments.Select(d => new UserDepartment
        {
            UserId = user.Id,
            DepartmentId = d.DepartmentId,
            IsPrimary = d.IsPrimary
        }).ToList();

        user.ReplaceDepartments(assignments);
        await _users.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Implement AssignUserSkillsCommand**

```csharp
// src/CRM.Application/Admin/Users/Commands/AssignUserSkillsCommand.cs
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record AssignUserSkillsCommand(
    Guid UserId,
    IReadOnlyList<Guid> CategoryIds) : IRequest;

public class AssignUserSkillsCommandHandler : IRequestHandler<AssignUserSkillsCommand>
{
    private readonly IUserRepository _users;
    private readonly ICategoryExistenceChecker _categories;

    public AssignUserSkillsCommandHandler(
        IUserRepository users,
        ICategoryExistenceChecker categories)
    {
        _users = users;
        _categories = categories;
    }

    public async Task Handle(AssignUserSkillsCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        if (cmd.CategoryIds.Count > 0)
        {
            bool allExist = await _categories.AllExistAsync(cmd.CategoryIds, ct);
            if (!allExist)
                throw new InvalidOperationException(
                    "One or more category IDs do not exist.");
        }

        // ⚠️ Uses raw SQL via ReplaceUserSkillsAsync instead of user.ReplaceSkills() + SaveChanges
        // because EF Core OwnsMany with private backing fields did not reliably track changes.
        await _users.ReplaceUserSkillsAsync(user.Id, cmd.CategoryIds, ct);
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AssignUserCommandHandlerTests" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 9: Add endpoints to AdminUsersController**

```csharp
// Add to src/CRM.API/Controllers/AdminUsersController.cs:

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

public record AssignDepartmentsRequest(
    IReadOnlyList<DepartmentAssignmentItem> Departments);
public record DepartmentAssignmentItem(Guid DepartmentId, bool IsPrimary);
public record AssignSkillsRequest(IReadOnlyList<Guid> CategoryIds);
```

- [ ] **Step 10: Write controller tests**

```csharp
// tests/CRM.API.Tests/Admin/AdminUsersControllerAssignTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerAssignTests
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
    public async Task AssignDepartments_MultiplePrimary_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AssignUserDepartmentsCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException(
                     "MULTIPLE_PRIMARY_DEPARTMENTS: Exactly one department must have isPrimary = true."));

        var response = await BuildClient().PutAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/departments",
            new
            {
                departments = new[]
                {
                    new { departmentId = Guid.NewGuid(), isPrimary = true },
                    new { departmentId = Guid.NewGuid(), isPrimary = true }
                }
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AssignSkills_UnknownCategory_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AssignUserSkillsCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException(
                     "One or more category IDs do not exist."));

        var response = await BuildClient().PutAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/skills",
            new { categoryIds = new[] { Guid.NewGuid() } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 11: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminUsersControllerAssignTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 12: Commit**

```bash
git add src/CRM.Domain/Users/UserSkill.cs \
        src/CRM.Application/Admin/Users/Commands/AssignUserDepartmentsCommand.cs \
        src/CRM.Application/Admin/Users/Commands/AssignUserSkillsCommand.cs \
        src/CRM.Application/Admin/Users/Commands/ICategoryExistenceChecker.cs \
        src/CRM.API/Controllers/AdminUsersController.cs \
        tests/CRM.Application.Tests/Admin/AssignUserCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminUsersControllerAssignTests.cs
git commit -m "feat(admin): add PUT /api/admin/users/{id}/departments and /skills — atomic assignment with validation"
```
