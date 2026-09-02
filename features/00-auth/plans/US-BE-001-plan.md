# Login Internal — Implementation Plan

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

**Story:** US-BE-001  
**Goal:** Implement `POST /api/v1/auth/login` — validates credentials for both staff **and** portal customers, issues JWT access token (15 min), sets HttpOnly refresh token cookie (7 days).

**Architecture:** `LoginInternalCommand` flows through MediatR pipeline (FluentValidation behavior runs first) → handler first tries to find a `User` (staff) by email; if none found, tries `Customer`; verifies BCrypt hash, checks `IsActive` and `RequiresPasswordChange` flags, calls `TokenService` to mint tokens; raw refresh token returned to controller, SHA-256 hash persisted in `RefreshTokens` table.

> **⚠️ Implementation divergences from original plan:**
> - Route is `/api/v1/auth/login` (not `/api/auth/login-internal`)
> - `LoginInternalCommand` handles customer logins too (not just staff)
> - `LoginResponse` DTO does **not** include `PrimaryDepartmentId` or `DepartmentIds`
> - `AuthController.Login` returns **flat** JSON (no nested `user` object); 423 is returned when `RequiresPasswordChange` is true, before reaching the Ok response
> - `ITokenService` has a second overload: `CreateAccessToken(Guid id, string email, string role, string fullName)`

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Auth/RefreshToken.cs` |
| Create | `src/CRM.Application/Auth/Commands/LoginInternalCommand.cs` |
| Create | `src/CRM.Application/Auth/DTOs/LoginResponse.cs` |
| Create | `src/CRM.Application/Auth/Validators/LoginInternalCommandValidator.cs` |
| Create | `src/CRM.Infrastructure/Identity/ITokenService.cs` |
| Create | `src/CRM.Infrastructure/Identity/TokenService.cs` |
| Create | `src/CRM.Infrastructure/Identity/JwtSettings.cs` |
| Create | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/LoginInternalCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerLoginTests.cs` |

---

## Task 1: RefreshToken Domain Entity

**Files:**
- Create: `src/CRM.Domain/Auth/RefreshToken.cs`

- [ ] **Step 1: Create the entity**

```csharp
// src/CRM.Domain/Auth/RefreshToken.cs
namespace CRM.Domain.Auth;

public class RefreshToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

    public void Revoke() => IsRevoked = true;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Auth/RefreshToken.cs
git commit -m "feat(domain): add RefreshToken entity"
```

---

## Task 2: TokenService (Infrastructure)

**Files:**
- Create: `src/CRM.Infrastructure/Identity/ITokenService.cs`
- Create: `src/CRM.Infrastructure/Identity/JwtSettings.cs`
- Create: `src/CRM.Infrastructure/Identity/TokenService.cs`

- [ ] **Step 1: Create JwtSettings and interface**

```csharp
// src/CRM.Infrastructure/Identity/JwtSettings.cs
namespace CRM.Infrastructure.Identity;

public class JwtSettings
{
    public string Secret { get; init; } = null!;
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public int AccessTokenMinutes { get; init; } = 15;
}
```

```csharp
// src/CRM.Infrastructure/Identity/ITokenService.cs
using CRM.Domain.Users;

namespace CRM.Infrastructure.Identity;

public interface ITokenService
{
    string CreateAccessToken(User user);
    (string RawToken, string TokenHash) CreateRefreshToken();
}
```

- [ ] **Step 2: Implement TokenService**

```csharp
// src/CRM.Infrastructure/Identity/TokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CRM.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CRM.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, string TokenHash) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return (raw, hash);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/CRM.Infrastructure/Identity/
git commit -m "feat(infra): add TokenService for JWT and refresh token generation"
```

---

## Task 3: LoginInternal Command + Handler + Validator + DTO

**Files:**
- Create: `src/CRM.Application/Auth/DTOs/LoginResponse.cs`
- Create: `src/CRM.Application/Auth/Commands/LoginInternalCommand.cs`
- Create: `src/CRM.Application/Auth/Validators/LoginInternalCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Auth/LoginInternalCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/LoginInternalCommandHandlerTests.cs
using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class LoginInternalCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly LoginInternalCommandHandler _handler;

    public LoginInternalCommandHandlerTests()
    {
        _handler = new LoginInternalCommandHandler(
            _userRepo.Object, _tokenService.Object, _refreshRepo.Object);
    }

    private static User MakeUser(bool isActive = true, bool requiresPasswordChange = false)
        => User.CreateForTest(
            email: "agent@crm.test",
            passwordHash: BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
            role: UserRole.Agent,
            isActive: isActive,
            requiresPasswordChange: requiresPasswordChange);

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsLoginResponse()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("access-jwt");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("raw", "hash"));

        var result = await _handler.Handle(
            new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), default);

        Assert.Equal("access-jwt", result.AccessToken);
        Assert.Equal("raw", result.RefreshToken);
        Assert.False(result.RequiresPasswordChange);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser();
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("agent@crm.test", "wrong"), default));
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepo.Setup(r => r.FindByEmailAsync("ghost@crm.test", default)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("ghost@crm.test", "any"), default));
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorizedAccessException()
    {
        var user = MakeUser(isActive: false);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), default));
    }

    [Fact]
    public async Task Handle_RequiresPasswordChange_FlagIsTrue()
    {
        var user = MakeUser(requiresPasswordChange: true);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);
        _tokenService.Setup(t => t.CreateAccessToken(user)).Returns("jwt");
        _tokenService.Setup(t => t.CreateRefreshToken()).Returns(("raw", "hash"));

        var result = await _handler.Handle(
            new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), default);

        Assert.True(result.RequiresPasswordChange);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "LoginInternalCommandHandlerTests" -v n
```

Expected: FAIL — `LoginInternalCommand` and `LoginInternalCommandHandler` do not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Auth/DTOs/LoginResponse.cs
namespace CRM.Application.Auth.DTOs;

// ⚠️ Implemented without PrimaryDepartmentId / DepartmentIds
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    bool RequiresPasswordChange,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);
```

- [ ] **Step 4: Create command and handler**

```csharp
// src/CRM.Application/Auth/Commands/LoginInternalCommand.cs
using CRM.Application.Auth.DTOs;
using CRM.Domain.Auth;
using CRM.Infrastructure.Identity;
using MediatR;

namespace CRM.Application.Auth.Commands;

public record LoginInternalCommand(string Email, string Password) : IRequest<LoginResponse>;

public class LoginInternalCommandHandler : IRequestHandler<LoginInternalCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly ITokenService _tokens;
    private readonly IRefreshTokenRepository _refreshTokens;

    public LoginInternalCommandHandler(
        IUserRepository users, ITokenService tokens, IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
    }

    public async Task<LoginResponse> Handle(LoginInternalCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        var accessToken = _tokens.CreateAccessToken(user);
        var (rawToken, tokenHash) = _tokens.CreateRefreshToken();

        var refreshToken = RefreshToken.Create(user.Id, tokenHash, DateTime.UtcNow.AddDays(7));
        await _refreshTokens.AddAsync(refreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: rawToken,
            RequiresPasswordChange: user.RequiresPasswordChange,
            UserId: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role.ToString());
    }
}
```

- [ ] **Step 5: Create validator**

```csharp
// src/CRM.Application/Auth/Validators/LoginInternalCommandValidator.cs
using CRM.Application.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public class LoginInternalCommandValidator : AbstractValidator<LoginInternalCommand>
{
    public LoginInternalCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "LoginInternalCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Auth/ tests/CRM.Application.Tests/Auth/LoginInternalCommandHandlerTests.cs
git commit -m "feat(auth): add LoginInternalCommand, handler, validator, and DTO"
```

---

## Task 4: AuthController — POST /api/auth/login-internal

**Files:**
- Create: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerLoginTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerLoginTests.cs
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

public class AuthControllerLoginTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(Action<IServiceCollection>? configure = null)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(_mediator.Object);
                configure?.Invoke(services);
            }));
        return factory.CreateClient();
    }

    [Fact]
    public async Task LoginInternal_ValidBody_Returns200WithAccessToken()
    {
        _mediator.Setup(m => m.Send(It.IsAny<LoginInternalCommand>(), default))
                 .ReturnsAsync(new LoginResponse("jwt", "raw-refresh", false,
                     Guid.NewGuid(), "Ali", "Hassan", "agent@crm.test", "Agent", null, Array.Empty<Guid>()));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/auth/login-internal",
            new { email = "agent@crm.test", password = "P@ssw0rd!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("jwt", body!["accessToken"].ToString());
    }

    [Fact]
    public async Task LoginInternal_WrongPassword_Returns401()
    {
        _mediator.Setup(m => m.Send(It.IsAny<LoginInternalCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/auth/login-internal",
            new { email = "agent@crm.test", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginInternal_ValidBody_SetsRefreshTokenCookie()
    {
        _mediator.Setup(m => m.Send(It.IsAny<LoginInternalCommand>(), default))
                 .ReturnsAsync(new LoginResponse("jwt", "raw-refresh", false,
                     Guid.NewGuid(), "Ali", "Hassan", "agent@crm.test", "Agent", null, Array.Empty<Guid>()));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/auth/login-internal",
            new { email = "agent@crm.test", password = "P@ssw0rd!" });

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("refreshToken="));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerLoginTests" -v n
```

Expected: FAIL — `AuthController` does not exist yet.

- [ ] **Step 3: Implement AuthController**

```csharp
// src/CRM.API/Controllers/AuthController.cs
// ⚠️ Route is /api/v1/auth (not /api/auth). Response is flat — no nested user object.
// RequiresPasswordChange → 423 before Ok; customer logins share this same endpoint.
using CRM.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    public sealed class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var command = new LoginInternalCommand(request.Email, request.Password);
            var result = await _mediator.Send(command, ct);

            Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            if (result.RequiresPasswordChange)
                return StatusCode(423, new { code = "PASSWORD_CHANGE_REQUIRED" });

            return Ok(new
            {
                result.AccessToken,
                result.RequiresPasswordChange,
                UserId = result.UserId,
                result.Email,
                result.FirstName,
                result.LastName,
                result.Role,
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { code = "INVALID_CREDENTIALS" });
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerLoginTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs tests/CRM.API.Tests/Auth/AuthControllerLoginTests.cs
git commit -m "feat(api): add POST /api/auth/login-internal with HttpOnly cookie"
```
