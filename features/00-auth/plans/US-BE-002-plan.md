# Token Refresh — Implementation Plan

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

**Story:** US-BE-002  
**Goal:** Implement `POST /api/auth/refresh` — reads HttpOnly refresh token cookie, validates it against the DB hash, rotates it, and returns a new JWT access token.

**Architecture:** `RefreshTokenCommand` carries the raw token from the cookie → handler looks up `RefreshToken` record by SHA-256 hash, checks `IsActive`, calls `TokenService` for new tokens, revokes old record, persists new one (rotation). Controller reads cookie, dispatches command, writes new cookie.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, System.IdentityModel.Tokens.Jwt, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Auth/Commands/RefreshTokenCommand.cs` |
| Create | `src/CRM.Application/Auth/DTOs/RefreshTokenResponse.cs` |
| Create | `src/CRM.Application/Auth/Validators/RefreshTokenCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerRefreshTests.cs` |

---

## Task 1: RefreshToken Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Auth/DTOs/RefreshTokenResponse.cs`
- Create: `src/CRM.Application/Auth/Commands/RefreshTokenCommand.cs`
- Create: `src/CRM.Application/Auth/Validators/RefreshTokenCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.Commands;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(
            _refreshRepo.Object, _userRepo.Object, _tokenService.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_ReturnsNewAccessToken()
    {
        const string raw = "valid-raw-token";
        var userId = Guid.NewGuid();
        var stored = RefreshToken.Create(userId, Hash(raw), DateTime.UtcNow.AddDays(7));
        var user = User.CreateForTest(email: "a@b.com", passwordHash: "x",
            role: UserRole.Agent, isActive: true, requiresPasswordChange: false);

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);
        _userRepo.Setup(r => r.FindByIdAsync(userId, default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("new-jwt");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("new-raw", "new-hash"));

        var result = await _handler.Handle(new RefreshTokenCommand(raw), default);

        Assert.Equal("new-jwt", result.AccessToken);
        Assert.Equal("new-raw", result.NewRefreshToken);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsUnauthorizedAccessException()
    {
        _refreshRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                    .ReturnsAsync((RefreshToken?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RefreshTokenCommand("bad"), default));
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsUnauthorizedAccessException()
    {
        const string raw = "revoked-token";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(7));
        stored.Revoke();

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RefreshTokenCommand(raw), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorizedAccessException()
    {
        const string raw = "expired-token";
        var stored = RefreshToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddDays(-1));

        _refreshRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(stored);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new RefreshTokenCommand(raw), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RefreshTokenCommandHandlerTests" -v n
```

Expected: FAIL — `RefreshTokenCommand` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Auth/DTOs/RefreshTokenResponse.cs
namespace CRM.Application.Auth.DTOs;

public record RefreshTokenResponse(string AccessToken, string NewRefreshToken);
```

- [ ] **Step 4: Create command and handler**

```csharp
// src/CRM.Application/Auth/Commands/RefreshTokenCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Auth.DTOs;
using CRM.Domain.Auth;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record RefreshTokenCommand(string RawToken) : IRequest<RefreshTokenResponse>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens, IUserRepository users, ITokenService tokens)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _tokens = tokens;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.RawToken)));
        var stored = await _refreshTokens.FindByHashAsync(hash, ct);

        if (stored is null || !stored.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await _users.FindByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        stored.Revoke();

        var accessToken = _tokens.CreateAccessToken(user);
        var (newRaw, newHash) = _tokens.CreateRefreshToken();

        var newToken = RefreshToken.Create(user.Id, newHash, DateTime.UtcNow.AddDays(7));
        await _refreshTokens.AddAsync(newToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new RefreshTokenResponse(accessToken, newRaw);
    }
}
```

- [ ] **Step 5: Create validator**

```csharp
// src/CRM.Application/Auth/Validators/RefreshTokenCommandValidator.cs
using CRM.Application.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RawToken).NotEmpty();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RefreshTokenCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Auth/ tests/CRM.Application.Tests/Auth/RefreshTokenCommandHandlerTests.cs
git commit -m "feat(auth): add RefreshTokenCommand with rotation logic"
```

---

## Task 2: AuthController — POST /api/auth/refresh

**Files:**
- Modify: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerRefreshTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerRefreshTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerRefreshTests
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
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    [Fact]
    public async Task Refresh_MissingCookie_Returns401()
    {
        var client = BuildClient();
        var response = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidCookie_Returns200WithNewToken()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
                 .ReturnsAsync(new RefreshTokenResponse("new-jwt", "new-raw"));

        var client = BuildClient();
        client.DefaultRequestHeaders.Add("Cookie", "refreshToken=valid-raw");

        var response = await client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("new-jwt", body!["accessToken"]);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerRefreshTests" -v n
```

Expected: FAIL — `/api/auth/refresh` endpoint does not exist yet.

- [ ] **Step 3: Add refresh endpoint to AuthController**

```csharp
// Add to src/CRM.API/Controllers/AuthController.cs inside the class:

[HttpPost("refresh")]
public async Task<IActionResult> Refresh(CancellationToken ct)
{
    var rawToken = Request.Cookies["refreshToken"];
    if (string.IsNullOrEmpty(rawToken))
        return Unauthorized(new { error = "Refresh token missing." });

    try
    {
        var result = await _mediator.Send(new RefreshTokenCommand(rawToken), ct);

        Response.Cookies.Append("refreshToken", result.NewRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Ok(new { accessToken = result.AccessToken });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerRefreshTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs tests/CRM.API.Tests/Auth/AuthControllerRefreshTests.cs
git commit -m "feat(api): add POST /api/auth/refresh with token rotation"
```
