# Get Current User — Implementation Plan

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

**Story:** US-BE-006  
**Goal:** Implement `GET /api/auth/me` — returns the authenticated user's profile from JWT claims, confirming the token is valid.

**Architecture:** `GetCurrentUserQuery` carries the `userId` extracted from `HttpContext.User` by the controller → handler fetches the `User` aggregate from the repository, maps to `CurrentUserDto`. Endpoint is protected by `[Authorize]`; 401 is returned automatically by the JWT middleware if the token is missing/invalid.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Auth/Queries/GetCurrentUserQuery.cs` |
| Create | `src/CRM.Application/Auth/DTOs/CurrentUserDto.cs` |
| Modify | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/GetCurrentUserQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerMeTests.cs` |

---

## Task 1: GetCurrentUser Query + Handler + DTO

**Files:**
- Create: `src/CRM.Application/Auth/DTOs/CurrentUserDto.cs`
- Create: `src/CRM.Application/Auth/Queries/GetCurrentUserQuery.cs`
- Test: `tests/CRM.Application.Tests/Auth/GetCurrentUserQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/GetCurrentUserQueryHandlerTests.cs
using CRM.Application.Auth.DTOs;
using CRM.Application.Auth.Queries;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class GetCurrentUserQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _handler = new GetCurrentUserQueryHandler(_userRepo.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsCurrentUserDto()
    {
        var userId = Guid.NewGuid();
        var user = User.CreateForTest(
            email: "manager@crm.test",
            passwordHash: "hash",
            role: UserRole.Manager,
            isActive: true,
            requiresPasswordChange: false,
            id: userId,
            firstName: "Sara",
            lastName: "Al-Ali");

        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        var result = await _handler.Handle(new GetCurrentUserQuery(userId), default);

        Assert.Equal(userId, result.Id);
        Assert.Equal("manager@crm.test", result.Email);
        Assert.Equal("Manager", result.Role);
        Assert.Equal("Sara", result.FirstName);
        Assert.Equal("Al-Ali", result.LastName);
        Assert.False(result.RequiresPasswordChange);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        _userRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                 .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCurrentUserQuery(Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetCurrentUserQueryHandlerTests" -v n
```

Expected: FAIL — `GetCurrentUserQuery` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Auth/DTOs/CurrentUserDto.cs
namespace CRM.Application.Auth.DTOs;

public record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsActive,
    bool RequiresPasswordChange,
    string? AvatarUrl,
    string? DepartmentName);
```

- [ ] **Step 4: Create query and handler**

```csharp
// src/CRM.Application/Auth/Queries/GetCurrentUserQuery.cs
using CRM.Application.Auth.DTOs;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<CurrentUserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IUserRepository _users;

    public GetCurrentUserQueryHandler(IUserRepository users) => _users = users;

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(query.UserId, ct)
            ?? throw new KeyNotFoundException($"User {query.UserId} not found.");

        return new CurrentUserDto(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role.ToString(),
            IsActive: user.IsActive,
            RequiresPasswordChange: user.RequiresPasswordChange,
            AvatarUrl: user.AvatarUrl,
            DepartmentName: user.Department?.Name);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetCurrentUserQueryHandlerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Auth/Queries/ src/CRM.Application/Auth/DTOs/CurrentUserDto.cs \
        tests/CRM.Application.Tests/Auth/GetCurrentUserQueryHandlerTests.cs
git commit -m "feat(auth): add GetCurrentUserQuery handler and DTO"
```

---

## Task 2: AuthController — GET /api/auth/me

**Files:**
- Modify: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerMeTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerMeTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Auth.DTOs;
using CRM.Application.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerMeTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(bool authenticated = true)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(_mediator.Object);
            }));
        var client = factory.CreateClient();
        if (authenticated)
        {
            // Test infrastructure: attach a test JWT with known sub claim
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwtHelper.CreateTestToken());
        }
        return client;
    }

    [Fact]
    public async Task GetMe_Authenticated_Returns200WithProfile()
    {
        var userId = TestJwtHelper.TestUserId;
        _mediator.Setup(m => m.Send(It.Is<GetCurrentUserQuery>(q => q.UserId == userId), default))
                 .ReturnsAsync(new CurrentUserDto(userId, "a@b.com", "Ali", "Hassan",
                     "Agent", true, false, null, "Support"));

        var client = BuildClient();
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("a@b.com", body!.Email);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var client = BuildClient(authenticated: false);
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerMeTests" -v n
```

Expected: FAIL — `GET /api/auth/me` does not exist yet.

- [ ] **Step 3: Add me endpoint to AuthController**

```csharp
// Add to src/CRM.API/Controllers/AuthController.cs inside the class:

[Authorize]
[HttpGet("me")]
public async Task<IActionResult> Me(CancellationToken ct)
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value;

    if (!Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    try
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(userId), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerMeTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs \
        tests/CRM.API.Tests/Auth/AuthControllerMeTests.cs
git commit -m "feat(api): add GET /api/auth/me endpoint"
```
