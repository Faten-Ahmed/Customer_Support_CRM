# Email Verification — Implementation Plan

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

**Story:** US-BE-011  
**Goal:** Implement `POST /api/portal/auth/verify-email` — validates the email verification token, marks the customer's email as verified, and marks the token as used.

**Architecture:** `VerifyEmailCommand(token)` → handler hashes raw token, finds `EmailVerificationToken` by hash, checks `IsValid`, fetches `CustomerCredential` by `CustomerId`, calls `credential.VerifyEmail()`, marks token used, saves. Always returns 200 to avoid token enumeration.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Portal/Auth/Commands/VerifyEmailCommand.cs` |
| Create | `src/CRM.Application/Portal/Auth/Validators/VerifyEmailCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/Portal/PortalAuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Portal/Auth/VerifyEmailCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Portal/PortalAuthControllerVerifyEmailTests.cs` |

---

## Task 1: VerifyEmail Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Portal/Auth/Commands/VerifyEmailCommand.cs`
- Create: `src/CRM.Application/Portal/Auth/Validators/VerifyEmailCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Portal/Auth/VerifyEmailCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Portal/Auth/VerifyEmailCommandHandlerTests.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Portal.Auth.Commands;
using CRM.Domain.Customers;
using CRM.Infrastructure.Repositories;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal.Auth;

public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IEmailVerificationTokenRepository> _tokenRepo = new();
    private readonly Mock<ICustomerCredentialRepository> _credRepo = new();
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _handler = new VerifyEmailCommandHandler(_tokenRepo.Object, _credRepo.Object);
    }

    private static string Hash(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    [Fact]
    public async Task Handle_ValidToken_VerifiesEmail()
    {
        const string raw = "valid-verify-token";
        var customerId = Guid.NewGuid();
        var token = EmailVerificationToken.Create(customerId, Hash(raw), DateTime.UtcNow.AddHours(24));
        var cred = CustomerCredential.Create(customerId, "hash");

        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(token);
        _credRepo.Setup(r => r.FindByCustomerIdAsync(customerId, default)).ReturnsAsync(cred);

        await _handler.Handle(new VerifyEmailCommand(raw), default);

        Assert.True(cred.IsEmailVerified);
        Assert.True(token.IsUsed);
        _credRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsInvalidOperationException()
    {
        _tokenRepo.Setup(r => r.FindByHashAsync(It.IsAny<string>(), default))
                  .ReturnsAsync((EmailVerificationToken?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new VerifyEmailCommand("bad-token"), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsInvalidOperationException()
    {
        const string raw = "expired";
        var token = EmailVerificationToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddHours(-1));
        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(token);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new VerifyEmailCommand(raw), default));
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ThrowsInvalidOperationException()
    {
        const string raw = "used-token";
        var token = EmailVerificationToken.Create(Guid.NewGuid(), Hash(raw), DateTime.UtcNow.AddHours(1));
        token.MarkUsed();
        _tokenRepo.Setup(r => r.FindByHashAsync(Hash(raw), default)).ReturnsAsync(token);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new VerifyEmailCommand(raw), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "VerifyEmailCommandHandlerTests" -v n
```

Expected: FAIL — `VerifyEmailCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Portal/Auth/Commands/VerifyEmailCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Domain.Customers;
using CRM.Infrastructure.Repositories;
using MediatR;

namespace CRM.Application.Portal.Auth.Commands;

public record VerifyEmailCommand(string Token) : IRequest;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly ICustomerCredentialRepository _credentials;

    public VerifyEmailCommandHandler(
        IEmailVerificationTokenRepository tokens,
        ICustomerCredentialRepository credentials)
    {
        _tokens = tokens;
        _credentials = credentials;
    }

    public async Task Handle(VerifyEmailCommand cmd, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cmd.Token)));
        var token = await _tokens.FindByHashAsync(hash, ct);

        if (token is null || !token.IsValid)
            throw new InvalidOperationException("Invalid or expired verification token.");

        var credential = await _credentials.FindByCustomerIdAsync(token.CustomerId, ct)
            ?? throw new InvalidOperationException("Customer credential not found.");

        credential.VerifyEmail();
        token.MarkUsed();

        await _credentials.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Portal/Auth/Validators/VerifyEmailCommandValidator.cs
using CRM.Application.Portal.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Portal.Auth.Validators;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "VerifyEmailCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Portal/Auth/Commands/VerifyEmailCommand.cs \
        src/CRM.Application/Portal/Auth/Validators/VerifyEmailCommandValidator.cs \
        tests/CRM.Application.Tests/Portal/Auth/VerifyEmailCommandHandlerTests.cs
git commit -m "feat(portal): add VerifyEmailCommand handler"
```

---

## Task 2: PortalAuthController — POST /api/portal/auth/verify-email

**Files:**
- Modify: `src/CRM.API/Controllers/Portal/PortalAuthController.cs`
- Test: `tests/CRM.API.Tests/Portal/PortalAuthControllerVerifyEmailTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Portal/PortalAuthControllerVerifyEmailTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalAuthControllerVerifyEmailTests
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
    public async Task VerifyEmail_ValidToken_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/portal/auth/verify-email",
            new { token = "valid-token" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Invalid token."));
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/portal/auth/verify-email",
            new { token = "bad" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalAuthControllerVerifyEmailTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add verify-email endpoint to PortalAuthController**

```csharp
// Add to src/CRM.API/Controllers/Portal/PortalAuthController.cs inside the class:

[HttpPost("verify-email")]
public async Task<IActionResult> VerifyEmail(
    [FromBody] VerifyEmailCommand command, CancellationToken ct)
{
    try
    {
        await _mediator.Send(command, ct);
        return Ok(new { message = "Email verified successfully. You may now log in." });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalAuthControllerVerifyEmailTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/Portal/PortalAuthController.cs \
        tests/CRM.API.Tests/Portal/PortalAuthControllerVerifyEmailTests.cs
git commit -m "feat(api): add POST /api/portal/auth/verify-email endpoint"
```
