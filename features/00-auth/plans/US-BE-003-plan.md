# Logout — Implementation Plan

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

**Story:** US-BE-003  
**Goal:** Implement `POST /api/auth/logout` — revokes the current refresh token in the DB and clears the HttpOnly cookie.

**Architecture:** `LogoutCommand` carries the raw refresh token from the cookie → handler hashes it, finds the `RefreshToken` record, calls `Revoke()`, persists. Controller reads cookie, dispatches command, clears cookie regardless of result.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Auth/Commands/LogoutCommand.cs` |
| Modify | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/LogoutCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerLogoutTests.cs` |

---

## Task 1: Logout Command + Handler

**Files:**
- Create: `src/CRM.Application/Auth/Commands/LogoutCommand.cs`
- Test: `tests/CRM.Application.Tests/Auth/LogoutCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/LogoutCommandHandlerTests.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refreshRepo.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_RevokesToken()
    {
        const string raw = "valid-raw";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(7));

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await _handler.Handle(new LogoutCommand(raw), default);

        Assert.True(stored.IsRevoked);
        _refreshRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_CompletesWithoutError()
    {
        _refreshRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                    .ReturnsAsync((RefreshToken?)null);

        // Should not throw — idempotent logout
        await _handler.Handle(new LogoutCommand("missing"), default);

        _refreshRepo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_CompletesWithoutError()
    {
        const string raw = "already-revoked";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(7));
        stored.Revoke();

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await _handler.Handle(new LogoutCommand(raw), default);

        // Already revoked — SaveChanges should still be called to persist idempotency
        _refreshRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "LogoutCommandHandlerTests" -v n
```

Expected: FAIL — `LogoutCommand` does not exist yet.

- [ ] **Step 3: Implement LogoutCommand and handler**

```csharp
// src/CRM.Application/Auth/Commands/LogoutCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record LogoutCommand(string RawToken) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens)
        => _refreshTokens = refreshTokens;

    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.RawToken)));
        var stored = await _refreshTokens.FindByHashAsync(hash, ct);

        if (stored is null)
            return;

        stored.Revoke();
        await _refreshTokens.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "LogoutCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Auth/Commands/LogoutCommand.cs \
        tests/CRM.Application.Tests/Auth/LogoutCommandHandlerTests.cs
git commit -m "feat(auth): add LogoutCommand with idempotent token revocation"
```

---

## Task 2: AuthController — POST /api/auth/logout

**Files:**
- Modify: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerLogoutTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerLogoutTests.cs
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerLogoutTests
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
    public async Task Logout_WithCookie_Returns204AndClearesCookie()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .Returns(Task.CompletedTask);

        var client = BuildClient();
        client.DefaultRequestHeaders.Add("Cookie", "refreshToken=raw-token");

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        // Cookie cleared: Set-Cookie header with empty value and past expiry
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.Contains("refreshToken=;") || c.Contains("refreshToken="));
    }

    [Fact]
    public async Task Logout_WithoutCookie_Returns204()
    {
        var client = BuildClient();
        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerLogoutTests" -v n
```

Expected: FAIL — `/api/auth/logout` endpoint does not exist yet.

- [ ] **Step 3: Add logout endpoint to AuthController**

```csharp
// Add to src/CRM.API/Controllers/AuthController.cs inside the class:

[HttpPost("logout")]
public async Task<IActionResult> Logout(CancellationToken ct)
{
    var rawToken = Request.Cookies["refreshToken"];

    if (!string.IsNullOrEmpty(rawToken))
        await _mediator.Send(new LogoutCommand(rawToken), ct);

    Response.Cookies.Delete("refreshToken");
    return NoContent();
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerLogoutTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs \
        tests/CRM.API.Tests/Auth/AuthControllerLogoutTests.cs
git commit -m "feat(api): add POST /api/auth/logout endpoint"
```
