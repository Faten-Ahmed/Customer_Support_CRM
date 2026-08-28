# Get, List, and Update Users — Implementation Plan

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

**Story:** US-BE-064  
**Goal:** Implement `GET /api/admin/users` (paginated list with filters), `GET /api/admin/users/{id}` (full profile with departments and skills), and `PUT /api/admin/users/{id}` (update fullName only — role changes are not allowed).

**Architecture:** `ListUsersQuery(Role?, DepartmentId?, IsActive?, Search, Page, PageSize)` → paginated list. `GetUserQuery(UserId)` → full profile including departments and skills. `UpdateUserCommand(UserId, FullName)` → updates name fields only. Existing `User` entity from US-BE-063 is used unchanged.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Admin/Users/Queries/ListUsersQuery.cs` |
| Create | `src/CRM.Application/Admin/Users/Queries/GetUserQuery.cs` |
| Create | `src/CRM.Application/Admin/Users/Commands/UpdateUserCommand.cs` |
| Create | `src/CRM.Application/Admin/Users/DTOs/UserDetailDto.cs` |
| Create | `src/CRM.Application/Admin/Users/DTOs/UserSummaryDto.cs` |
| Modify | `src/CRM.Domain/Users/IUserRepository.cs` |
| Modify | `src/CRM.API/Controllers/AdminUsersController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/UserQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminUsersControllerGetTests.cs` |

---

## Task 1: User Queries + Update Command

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/UserQueryHandlerTests.cs
using CRM.Application.Admin.Users.Commands;
using CRM.Application.Admin.Users.Queries;
using CRM.Application.Common;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class UserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly ListUsersQueryHandler _listHandler;
    private readonly GetUserQueryHandler _getHandler;
    private readonly UpdateUserCommandHandler _updateHandler;

    public UserQueryHandlerTests()
    {
        _listHandler = new ListUsersQueryHandler(_repo.Object);
        _getHandler = new GetUserQueryHandler(_repo.Object);
        _updateHandler = new UpdateUserCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task List_ReturnsPagedUsers()
    {
        _repo.Setup(r => r.ListAsync(null, null, null, null, 1, 20, default))
             .ReturnsAsync(new PagedResult<UserSummaryProjection>(
                 new List<UserSummaryProjection>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListUsersQuery(null, null, null, null, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Get_UserFound_ReturnsDetailDto()
    {
        var userId = Guid.NewGuid();
        var projection = new UserDetailProjection(
            userId, "Ahmed Al-Farsi", "ahmed@test.com", "Agent",
            true, false, "Offline", DateTime.UtcNow,
            new List<DepartmentAssignmentProjection>(),
            new List<SkillProjection>());

        _repo.Setup(r => r.GetDetailAsync(userId, default)).ReturnsAsync(projection);

        var result = await _getHandler.Handle(new GetUserQuery(userId), default);

        Assert.Equal("ahmed@test.com", result.Email);
    }

    [Fact]
    public async Task Get_UserNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.GetDetailAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((UserDetailProjection?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _getHandler.Handle(new GetUserQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Update_ValidUser_UpdatesFullName()
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Ahmed", "Old", "a@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _updateHandler.Handle(
            new UpdateUserCommand(user.Id, "Ahmed Updated"), default);

        Assert.Equal("Ahmed Updated", result.FullName);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UserQueryHandlerTests" -v n
```

Expected: FAIL — queries do not exist yet.

- [ ] **Step 3: Create DTOs and projections**

```csharp
// src/CRM.Application/Admin/Users/DTOs/UserSummaryDto.cs
namespace CRM.Application.Admin.Users.DTOs;

public record UserSummaryDto(
    Guid Id, string FullName, string Email, string Role,
    bool IsActive, string AvailabilityStatus, DateTime CreatedAt,
    Guid? PrimaryDepartmentId, string? PrimaryDepartmentName);
```

```csharp
// src/CRM.Application/Admin/Users/DTOs/UserDetailDto.cs
namespace CRM.Application.Admin.Users.DTOs;

public record UserDetailDto(
    Guid Id, string FullName, string Email, string Role,
    bool IsActive, bool PasswordMustChange, string AvailabilityStatus,
    DateTime CreatedAt,
    IReadOnlyList<DepartmentAssignmentDto> Departments,
    IReadOnlyList<SkillDto> Skills);

public record DepartmentAssignmentDto(Guid DepartmentId, string DepartmentName, bool IsPrimary);
public record SkillDto(Guid CategoryId, string CategoryName);
```

- [ ] **Step 4: Add projections and repository methods**

Add to `src/CRM.Domain/Users/IUserRepository.cs`:

```csharp
public record UserSummaryProjection(
    Guid Id, string FullName, string Email, string Role,
    bool IsActive, string AvailabilityStatus, DateTime CreatedAt,
    Guid? PrimaryDepartmentId, string? PrimaryDepartmentName);

public record UserDetailProjection(
    Guid Id, string FullName, string Email, string Role,
    bool IsActive, bool PasswordMustChange, string AvailabilityStatus, DateTime CreatedAt,
    IReadOnlyList<DepartmentAssignmentProjection> Departments,
    IReadOnlyList<SkillProjection> Skills);

public record DepartmentAssignmentProjection(Guid DepartmentId, string DepartmentName, bool IsPrimary);
public record SkillProjection(Guid CategoryId, string CategoryName);

Task<PagedResult<UserSummaryProjection>> ListAsync(
    UserRole? role, Guid? departmentId, bool? isActive, string? search,
    int page, int pageSize, CancellationToken ct = default);

Task<UserDetailProjection?> GetDetailAsync(Guid userId, CancellationToken ct = default);
```

- [ ] **Step 5: Implement ListUsersQuery**

```csharp
// src/CRM.Application/Admin/Users/Queries/ListUsersQuery.cs
using CRM.Application.Admin.Users.DTOs;
using CRM.Application.Common;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Queries;

public record ListUsersQuery(
    UserRole? Role, Guid? DepartmentId, bool? IsActive,
    string? Search, int Page, int PageSize)
    : IRequest<PagedResult<UserSummaryDto>>;

public class ListUsersQueryHandler
    : IRequestHandler<ListUsersQuery, PagedResult<UserSummaryDto>>
{
    private readonly IUserRepository _users;

    public ListUsersQueryHandler(IUserRepository users) => _users = users;

    public async Task<PagedResult<UserSummaryDto>> Handle(
        ListUsersQuery query, CancellationToken ct)
    {
        var paged = await _users.ListAsync(
            query.Role, query.DepartmentId, query.IsActive,
            query.Search, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(p => new UserSummaryDto(
                p.Id, p.FullName, p.Email, p.Role, p.IsActive,
                p.AvailabilityStatus, p.CreatedAt,
                p.PrimaryDepartmentId, p.PrimaryDepartmentName))
            .ToList();

        return new PagedResult<UserSummaryDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 6: Implement GetUserQuery**

```csharp
// src/CRM.Application/Admin/Users/Queries/GetUserQuery.cs
using CRM.Application.Admin.Users.DTOs;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Queries;

public record GetUserQuery(Guid UserId) : IRequest<UserDetailDto>;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDetailDto>
{
    private readonly IUserRepository _users;

    public GetUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<UserDetailDto> Handle(GetUserQuery query, CancellationToken ct)
    {
        var projection = await _users.GetDetailAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

        var departments = projection.Departments
            .Select(d => new DepartmentAssignmentDto(d.DepartmentId, d.DepartmentName, d.IsPrimary))
            .ToList();

        var skills = projection.Skills
            .Select(s => new SkillDto(s.CategoryId, s.CategoryName))
            .ToList();

        return new UserDetailDto(
            projection.Id, projection.FullName, projection.Email, projection.Role,
            projection.IsActive, projection.PasswordMustChange, projection.AvailabilityStatus,
            projection.CreatedAt, departments, skills);
    }
}
```

- [ ] **Step 7: Implement UpdateUserCommand**

```csharp
// src/CRM.Application/Admin/Users/Commands/UpdateUserCommand.cs
using CRM.Application.Admin.Users.DTOs;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Admin.Users.Commands;

public record UpdateUserCommand(Guid UserId, string FullName) : IRequest<UserProfileDto>;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserProfileDto>
{
    private readonly IUserRepository _users;

    public UpdateUserCommandHandler(IUserRepository users) => _users = users;

    public async Task<UserProfileDto> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.UserId} not found.");

        user.UpdateProfile(cmd.FullName);
        await _users.SaveChangesAsync(ct);

        return CreateInternalUserCommandHandler.Map(user);
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UserQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 9: Add endpoints to AdminUsersController**

```csharp
// Add to src/CRM.API/Controllers/AdminUsersController.cs:

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
        var result = await _mediator.Send(new UpdateUserCommand(id, req.FullName), ct);
        return Ok(new { data = result });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
}

public record UpdateUserRequest(string FullName);
```

- [ ] **Step 10: Write controller tests**

```csharp
// tests/CRM.API.Tests/Admin/AdminUsersControllerGetTests.cs
using System.Net;
using CRM.Application.Admin.Users.Queries;
using CRM.Application.Common;
using CRM.Application.Admin.Users.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerGetTests
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
    public async Task List_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListUsersQuery>(), default))
                 .ReturnsAsync(new PagedResult<UserSummaryDto>(
                     new List<UserSummaryDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), default))
                 .ThrowsAsync(new KeyNotFoundException("User not found."));

        var response = await BuildClient()
            .GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 11: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminUsersControllerGetTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 12: Commit**

```bash
git add src/CRM.Application/Admin/Users/Queries/ \
        src/CRM.Application/Admin/Users/Commands/UpdateUserCommand.cs \
        src/CRM.Application/Admin/Users/DTOs/ \
        src/CRM.API/Controllers/AdminUsersController.cs \
        tests/CRM.Application.Tests/Admin/UserQueryHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminUsersControllerGetTests.cs
git commit -m "feat(admin): add GET/PUT /api/admin/users — list, get-by-id, and update user profile"
```
