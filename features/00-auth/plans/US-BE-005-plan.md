# Reset Password — Implementation Plan

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

**Story:** US-BE-005  
**Goal:** Implement `POST /api/auth/reset-password` — validates the reset token, hashes the new password with BCrypt, updates the user, marks the token used, clears `RequiresPasswordChange` if set.

**Architecture:** `ResetPasswordCommand(token, newPassword)` → handler hashes the raw token, finds `PasswordResetToken` by hash, checks `IsValid`, fetches user, calls `user.SetPassword(BCrypt.HashPassword(newPassword))`, marks token used, clears `RequiresPasswordChange`, revokes all active refresh tokens for user (security).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, BCrypt.Net-Next, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Auth/Commands/ResetPasswordCommand.cs` |
| Create | `src/CRM.Application/Auth/Validators/ResetPasswordCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/ResetPasswordCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerResetPasswordTests.cs` |

---

## Task 1: ResetPassword Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Auth/Commands/ResetPasswordCommand.cs`
- Create: `src/CRM.Application/Auth/Validators/ResetPasswordCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Auth/ResetPasswordCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/ResetPasswordCommandHandlerTests.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(
            _tokenRepo.Object, _userRepo.Object, _refreshRepo.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordAndMarksTokenUsed()
    {
        const string raw = "valid-reset-token";
        var userId = Guid.NewGuid();
        var prt = PasswordResetToken.Create(userId, Hash(raw), DateTime.UtcNow.AddHours(1));
        var user = User.CreateForTest("a@b.com", "oldhash", UserRole.Agent, true, false);

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default);

        Assert.True(prt.IsUsed);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewP@ss1!", user.PasswordHash));
        _userRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsInvalidOperationException()
    {
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((PasswordResetToken?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ResetPasswordCommand("bad", "NewP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsInvalidOperationException()
    {
        const string raw = "expired";
        var prt = PasswordResetToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddHours(-1));

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_UsedToken_ThrowsInvalidOperationException()
    {
        const string raw = "used-token";
        var prt = PasswordResetToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddHours(1));
        prt.MarkUsed();

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default));
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesAllRefreshTokensForUser()
    {
        const string raw = "valid-for-revoke";
        var userId = Guid.NewGuid();
        var prt = PasswordResetToken.Create(userId, Hash(raw), DateTime.UtcNow.AddHours(1));
        var user = User.CreateForTest("a@b.com", "oldhash", UserRole.Agent, true, false);

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(prt);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);

        await _handler.Handle(new ResetPasswordCommand(raw, "NewP@ss1!"), default);

        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(userId, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ResetPasswordCommandHandlerTests" -v n
```

Expected: FAIL — `ResetPasswordCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Auth/Commands/ResetPasswordCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokens,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens)
    {
        _tokens = tokens;
        _users = users;
        _refreshTokens = refreshTokens;
    }

    public async Task Handle(ResetPasswordCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.Token)));
        var prt = await _tokens.FindByHashAsync(hash, ct);

        if (prt is null || !prt.IsValid)
            throw new InvalidOperationException("Invalid or expired password reset token.");

        var user = await _users.FindByIdAsync(prt.UserId, ct)
            ?? throw new InvalidOperationException("User not found.");

        user.SetPassword(BCrypt.Net.BCrypt.HashPassword(cmd.NewPassword));
        prt.MarkUsed();

        await _refreshTokens.RevokeAllForUserAsync(user.Id, ct);
        await _users.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Auth/Validators/ResetPasswordCommandValidator.cs
using CRM.Application.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^a-zA-Z\d]").WithMessage("Password must contain at least one special character.");
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ResetPasswordCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Auth/Commands/ResetPasswordCommand.cs \
        src/CRM.Application/Auth/Validators/ResetPasswordCommandValidator.cs \
        tests/CRM.Application.Tests/Auth/ResetPasswordCommandHandlerTests.cs
git commit -m "feat(auth): add ResetPasswordCommand with token validation and refresh revocation"
```

---

## Task 2: AuthController — POST /api/auth/reset-password

**Files:**
- Modify: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerResetPasswordTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerResetPasswordTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerResetPasswordTests
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
        return factory.CreateClient();
    }

    [Fact]
    public async Task ResetPassword_ValidBody_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { token = "valid-token", newPassword = "NewP@ss1!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Invalid or expired token."));
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { token = "bad", newPassword = "NewP@ss1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_Returns400()
    {
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/reset-password",
            new { token = "t", newPassword = "weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerResetPasswordTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add reset-password endpoint to AuthController**

```csharp
// Add to src/CRM.API/Controllers/AuthController.cs inside the class:

[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword(
    [FromBody] ResetPasswordCommand command, CancellationToken ct)
{
    try
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerResetPasswordTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs \
        tests/CRM.API.Tests/Auth/AuthControllerResetPasswordTests.cs
git commit -m "feat(api): add POST /api/auth/reset-password endpoint"
```
