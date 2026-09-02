# Forgot Password — Implementation Plan

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

**Story:** US-BE-004  
**Goal:** Implement `POST /api/auth/forgot-password` — generates a password-reset token, persists its hash, and sends a reset link via email. Always returns 200 to prevent user enumeration.

**Architecture:** `ForgotPasswordCommand` → handler looks up user by email (silent if not found), generates cryptographic token via `RandomNumberGenerator`, stores SHA-256 hash + 1-hour expiry in `PasswordResetTokens` table, dispatches email via `IEmailService`. Controller always returns 200.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Auth/PasswordResetToken.cs` |
| Create | `src/CRM.Application/Auth/Commands/ForgotPasswordCommand.cs` |
| Create | `src/CRM.Application/Auth/Validators/ForgotPasswordCommandValidator.cs` |
| Create | `src/CRM.Application/Common/IEmailService.cs` |
| Modify | `src/CRM.API/Controllers/AuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/ForgotPasswordCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Auth/AuthControllerForgotPasswordTests.cs` |

---

## Task 1: PasswordResetToken Domain Entity

**Files:**
- Create: `src/CRM.Domain/Auth/PasswordResetToken.cs`

- [ ] **Step 1: Create entity**

```csharp
// src/CRM.Domain/Auth/PasswordResetToken.cs
namespace CRM.Domain.Auth;

public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTime expiresAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

    public void MarkUsed() => IsUsed = true;

    public bool IsValid => !IsUsed && DateTime.UtcNow < ExpiresAt;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Auth/PasswordResetToken.cs
git commit -m "feat(domain): add PasswordResetToken entity"
```

---

## Task 2: IEmailService Interface

**Files:**
- Create: `src/CRM.Application/Common/IEmailService.cs`

- [ ] **Step 1: Define interface**

```csharp
// src/CRM.Application/Common/IEmailService.cs
namespace CRM.Application.Common;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(
        string toEmail, string toName, string resetLink, CancellationToken ct = default);

    Task SendEmailVerificationAsync(
        string toEmail, string toName, string verificationLink, CancellationToken ct = default);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Application/Common/IEmailService.cs
git commit -m "feat(application): add IEmailService interface"
```

---

## Task 3: ForgotPassword Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Auth/Commands/ForgotPasswordCommand.cs`
- Create: `src/CRM.Application/Auth/Validators/ForgotPasswordCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Auth/ForgotPasswordCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/ForgotPasswordCommandHandlerTests.cs
using CRM.Application.Auth.Commands;
using CRM.Application.Common;
using CRM.Domain.Auth;
using CRM.Domain.Users;
using CRM.Infrastructure.Identity;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordResetTokenRepository> _tokenRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(
            _userRepo.Object, _tokenRepo.Object, _emailService.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_SendsResetEmail()
    {
        var user = User.CreateForTest("agent@crm.test", "hash", UserRole.Agent, true, false);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        await _handler.Handle(new ForgotPasswordCommand("agent@crm.test"), default);

        _emailService.Verify(e => e.SendPasswordResetEmailAsync(
            "agent@crm.test", It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
        _tokenRepo.Verify(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownEmail_DoesNotSendEmail()
    {
        _userRepo.Setup(r => r.FindByEmailAsync("ghost@crm.test", default))
                 .ReturnsAsync((User?)null);

        // Silent — no error, no email (prevents enumeration)
        await _handler.Handle(new ForgotPasswordCommand("ghost@crm.test"), default);

        _emailService.Verify(e => e.SendPasswordResetEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingUser_TokenExpiresInOneHour()
    {
        var user = User.CreateForTest("agent@crm.test", "hash", UserRole.Agent, true, false);
        _userRepo.Setup(r => r.FindByEmailAsync("agent@crm.test", default)).ReturnsAsync(user);

        PasswordResetToken? captured = null;
        _tokenRepo.Setup(r => r.AddAsync(It.IsAny<PasswordResetToken>(), default))
                  .Callback<PasswordResetToken, CancellationToken>((t, _) => captured = t)
                  .Returns(Task.CompletedTask);

        await _handler.Handle(new ForgotPasswordCommand("agent@crm.test"), default);

        Assert.NotNull(captured);
        Assert.True(captured!.ExpiresAt > DateTime.UtcNow.AddMinutes(55));
        Assert.True(captured.ExpiresAt < DateTime.UtcNow.AddMinutes(65));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ForgotPasswordCommandHandlerTests" -v n
```

Expected: FAIL — `ForgotPasswordCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Auth/Commands/ForgotPasswordCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Common;
using CRM.Domain.Auth;
using CRM.Infrastructure.Identity;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CRM.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IEmailService _email;
    private readonly string _resetBaseUrl;

    public ForgotPasswordCommandHandler(
        IUserRepository users,
        IPasswordResetTokenRepository tokens,
        IEmailService email,
        IConfiguration? config = null)
    {
        _users = users;
        _tokens = tokens;
        _email = email;
        _resetBaseUrl = config?["App:FrontendUrl"] ?? "https://app.crm.local";
    }

    public async Task Handle(ForgotPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByEmailAsync(cmd.Email, ct);
        if (user is null)
            return; // Silent — no enumeration

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

        var token = PasswordResetToken.Create(user.Id, hash, DateTime.UtcNow.AddHours(1));
        await _tokens.AddAsync(token, ct);
        await _tokens.SaveChangesAsync(ct);

        var link = $"{_resetBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(raw)}";
        await _email.SendPasswordResetEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", link, ct);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Auth/Validators/ForgotPasswordCommandValidator.cs
using CRM.Application.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Auth.Validators;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ForgotPasswordCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Auth/Commands/ForgotPasswordCommand.cs \
        src/CRM.Application/Auth/Validators/ForgotPasswordCommandValidator.cs \
        tests/CRM.Application.Tests/Auth/ForgotPasswordCommandHandlerTests.cs
git commit -m "feat(auth): add ForgotPasswordCommand with email dispatch"
```

---

## Task 4: AuthController — POST /api/auth/forgot-password

**Files:**
- Modify: `src/CRM.API/Controllers/AuthController.cs`
- Test: `tests/CRM.API.Tests/Auth/AuthControllerForgotPasswordTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Auth/AuthControllerForgotPasswordTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerForgotPasswordTests
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
    public async Task ForgotPassword_AnyEmail_Always200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "whoever@crm.test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_Returns400()
    {
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerForgotPasswordTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add forgot-password endpoint to AuthController**

```csharp
// Add to src/CRM.API/Controllers/AuthController.cs inside the class:

[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword(
    [FromBody] ForgotPasswordCommand command, CancellationToken ct)
{
    await _mediator.Send(command, ct);
    return Ok(new { message = "If the email exists, a reset link has been sent." });
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AuthControllerForgotPasswordTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/AuthController.cs \
        tests/CRM.API.Tests/Auth/AuthControllerForgotPasswordTests.cs
git commit -m "feat(api): add POST /api/auth/forgot-password (enumeration-safe)"
```
