# First Login Password Change — Implementation Plan

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

**Story:** US-BE-008  
**Goal:** Implement `POST /api/auth/change-password-first-login` — allows a user with `RequiresPasswordChange = true` to set a new password; clears the flag and revokes all existing refresh tokens.

**Architecture:** `ChangeFirstLoginPasswordCommand(currentPassword, newPassword)` → requires `[Authorize]`; handler verifies current password (BCrypt), enforces that `RequiresPasswordChange` is true, hashes and sets new password, clears flag, revokes all refresh tokens for user. Also serves users who know their current password and want to change it voluntarily.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, BCrypt.Net-Next, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Auth/Commands/ChangeFirstLoginPasswordCommand.cs` |
| Create | `src/CRM.Application/Auth/Validators/ChangeFirstLoginPasswordCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/ChangeFirstLoginPasswordCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerChangeFirstLoginPasswordTests.cs` |

---

## Task 1: ChangeFirstLoginPassword Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Auth/Commands/ChangeFirstLoginPasswordCommand.cs`
- Create: `src/CRM.Application/Auth/Validators/ChangeFirstLoginPasswordCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Auth/ChangeFirstLoginPasswordCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/ChangeFirstLoginPasswordCommandHandlerTests.cs
using CRM.Application.Auth.Commands;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class ChangeFirstLoginPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly ChangeFirstLoginPasswordCommandHandler _handler;

    public ChangeFirstLoginPasswordCommandHandlerTests()
    {
        _handler = new ChangeFirstLoginPasswordCommandHandler(
            _userRepo.Object, _refreshRepo.Object);
    }

    private static User MakeUser(bool requiresChange = true)
        => User.CreateForTest(
            email: "newagent@crm.test",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("OldP@ss1!"),
            role: UserRole.Agent,
            isActive: true,
            requiresPasswordChange: requiresChange);

    [Fact]
    public async Task Handle_ValidCurrentPassword_ChangesPasswordAndClearsFlag()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser(requiresChange: true);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await _handler.Handle(
            new ChangeFirstLoginPasswordCommand(userId, "OldP@ss1!", "NewP@ss2!"), default);

        Assert.False(user.RequiresPasswordChange);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewP@ss2!", user.PasswordHash));
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ChangeFirstLoginPasswordCommand(userId, "WrongP@ss!", "NewP@ss2!"), default));
    }

    [Fact]
    public async Task Handle_SamePassword_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ChangeFirstLoginPasswordCommand(userId, "OldP@ss1!", "OldP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_ValidPassword_RevokesAllRefreshTokens()
    {
        var userId = Guid.NewGuid();
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await _handler.Handle(
            new ChangeFirstLoginPasswordCommand(userId, "OldP@ss1!", "NewP@ss2!"), default);

        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(userId, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChangeFirstLoginPasswordCommandHandlerTests" -v n
```

Expected: FAIL — `ChangeFirstLoginPasswordCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Auth/Commands/ChangeFirstLoginPasswordCommand.cs
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record ChangeFirstLoginPasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest;

public class ChangeFirstLoginPasswordCommandHandler
    : IRequestHandler<ChangeFirstLoginPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public ChangeFirstLoginPasswordCommandHandler(
        IUserRepository users, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(ChangeFirstLoginPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(cmd.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        if (BCrypt.Net.BCrypt.Verify(cmd.NewPassword, user.PasswordHash))
            throw new InvalidOperationException("New password must differ from the current password.");

        user.SetPassword(BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword));
        user.ClearRequiresPasswordChange();

        await _refreshTokens.RevokeAllForUserAsync(user.Id, ct);
        await _users.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Auth/Validators/ChangeFirstLoginPasswordCommandValidator.cs
using CRM.Application.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public class ChangeFirstLoginPasswordCommandValidator
    : AbstractValidator<ChangeFirstLoginPasswordCommand>
{
    public ChangeFirstLoginPasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Must contain a lowercase letter.")
            .Matches(@"\d").WithMessage("Must contain a digit.")
            .Matches(@"[^a-zA-Z\d]").WithMessage("Must contain a special character.");
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChangeFirstLoginPasswordCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Auth/Commands/ChangeFirstLoginPasswordCommand.cs \
        src/CRM.Application/Auth/Validators/ChangeFirstLoginPasswordCommandValidator.cs \
        tests/CRM.Application.Tests/Auth/ChangeFirstLoginPasswordCommandHandlerTests.cs
git commit -m "feat(auth): add ChangeFirstLoginPasswordCommand"
```

---

## Task 2: AuthController — POST /api/auth/change-password-first-login

**Files:**
- Modify: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerChangeFirstLoginPasswordTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerChangeFirstLoginPasswordTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerChangeFirstLoginPasswordTests
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
                "Bearer", TestJwtHelper.CreateTestToken());
        return client;
    }

    [Fact]
    public async Task ChangeFirstLoginPassword_ValidBody_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/change-password-first-login",
            new { currentPassword = "OldP@ss1!", newPassword = "NewP@ss2!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeFirstLoginPassword_WrongPassword_Returns401()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Wrong password"));
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/change-password-first-login",
            new { currentPassword = "wrong", newPassword = "NewP@ss2!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeFirstLoginPassword_Unauthenticated_Returns401()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/change-password-first-login",
            new { currentPassword = "OldP@ss1!", newPassword = "NewP@ss2!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerChangeFirstLoginPasswordTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add endpoint to AuthController**

```csharp
// Add to src/CRM.API/Controllers/AuthController.cs inside the class:

[Authorize]
[HttpPost("change-password-first-login")]
public async Task<IActionResult> ChangeFirstLoginPassword(
    [FromBody] ChangeFirstLoginPasswordRequest request,
    CancellationToken ct)
{
    var userIdClaim = User.FindFirst("sub")?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    try
    {
        await _mediator.Send(
            new ChangeFirstLoginPasswordCommand(userId, request.CurrentPassword, request.NewPassword),
            ct);
        return NoContent();
    }
    catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
}

// Nested request DTO (controller-only, not a MediatR command)
public record ChangeFirstLoginPasswordRequest(string CurrentPassword, string NewPassword);
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerChangeFirstLoginPasswordTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs \
        tests/CRM.API.Tests/Auth/AuthControllerChangeFirstLoginPasswordTests.cs
git commit -m "feat(api): add POST /api/auth/change-password-first-login endpoint"
```
