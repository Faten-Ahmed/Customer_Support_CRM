# Portal Self Registration — Implementation Plan

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

**Story:** US-BE-010  
**Goal:** Implement `POST /api/portal/auth/register` — lets a customer self-register with name, email, and password; creates the customer record with `IsEmailVerified = false` and sends a verification email.

**Architecture:** `RegisterCustomerCommand` → handler checks email uniqueness across both Customers and portal credentials, creates `Customer` aggregate, creates `CustomerCredential` record (BCrypt-hashed password), generates email verification token, sends verification email via `IEmailService`. No JWT issued yet — customer must verify email before logging in.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, BCrypt.Net-Next, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Customers/CustomerCredential.cs` |
| Create | `src/CRM.Domain/Customers/EmailVerificationToken.cs` |
| Create | `src/CRM.Application/Portal/Auth/Commands/RegisterCustomerCommand.cs` |
| Create | `src/CRM.Application/Portal/Auth/Validators/RegisterCustomerCommandValidator.cs` |
| Create | `src/CRM.API/Controllers/Portal/PortalAuthController.cs` |
| Test   | `tests/CRM.Application.Tests/Portal/Auth/RegisterCustomerCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Portal/PortalAuthControllerRegisterTests.cs` |

---

## Task 1: CustomerCredential + EmailVerificationToken Domain Entities

**Files:**
- Create: `src/CRM.Domain/Customers/CustomerCredential.cs`
- Create: `src/CRM.Domain/Customers/EmailVerificationToken.cs`

- [ ] **Step 1: Create entities**

```csharp
// src/CRM.Domain/Customers/CustomerCredential.cs
namespace CRM.Domain.Customers;

public class CustomerCredential
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PasswordHash { get; private set; } = null!;
    public bool IsEmailVerified { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CustomerCredential() { }

    public static CustomerCredential Create(Guid customerId, string passwordHash)
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

```csharp
// src/CRM.Domain/Customers/EmailVerificationToken.cs
namespace CRM.Domain.Customers;

public class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid customerId, string tokenHash, DateTime expiresAt)
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
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
git add src/CRM.Domain/Customers/CustomerCredential.cs \
        src/CRM.Domain/Customers/EmailVerificationToken.cs
git commit -m "feat(domain): add CustomerCredential and EmailVerificationToken entities"
```

---

## Task 2: RegisterCustomer Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Portal/Auth/Commands/RegisterCustomerCommand.cs`
- Create: `src/CRM.Application/Portal/Auth/Validators/RegisterCustomerCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Portal/Auth/RegisterCustomerCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Portal/Auth/RegisterCustomerCommandHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Portal.Auth.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal.Auth;

public class RegisterCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ICustomerCredentialRepository> _credRepo = new();
    private readonly Mock<IEmailVerificationTokenRepository> _tokenRepo = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly RegisterCustomerCommandHandler _handler;

    public RegisterCustomerCommandHandlerTests()
    {
        _handler = new RegisterCustomerCommandHandler(
            _customerRepo.Object, _credRepo.Object, _tokenRepo.Object, _emailService.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesCustomerAndSendsVerificationEmail()
    {
        _customerRepo.Setup(r => r.FindByEmailAsync("new@portal.test", default))
                     .ReturnsAsync((Customer?)null);

        await _handler.Handle(
            new RegisterCustomerCommand("Ali", "Nasser", "new@portal.test", "P@ssword1!"),
            default);

        _customerRepo.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Once);
        _credRepo.Verify(r => r.AddAsync(It.IsAny<CustomerCredential>(), default), Times.Once);
        _emailService.Verify(e => e.SendEmailVerificationAsync(
            "new@portal.test", It.IsAny<string>(), It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var existing = Customer.Create("Old", "User", "dup@portal.test");
        _customerRepo.Setup(r => r.FindByEmailAsync("dup@portal.test", default))
                     .ReturnsAsync(existing);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new RegisterCustomerCommand("Ali", "Nasser", "dup@portal.test", "P@ssword1!"),
                default));
    }

    [Fact]
    public async Task Handle_NewRegistration_PasswordIsHashed()
    {
        _customerRepo.Setup(r => r.FindByEmailAsync("hash@portal.test", default))
                     .ReturnsAsync((Customer?)null);

        CustomerCredential? captured = null;
        _credRepo.Setup(r => r.AddAsync(It.IsAny<CustomerCredential>(), default))
                 .Callback<CustomerCredential, CancellationToken>((c, _) => captured = c)
                 .Returns(Task.CompletedTask);

        await _handler.Handle(
            new RegisterCustomerCommand("Ali", "Test", "hash@portal.test", "PlainP@ss1!"),
            default);

        Assert.NotNull(captured);
        Assert.NotEqual("PlainP@ss1!", captured!.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("PlainP@ss1!", captured.PasswordHash));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RegisterCustomerCommandHandlerTests" -v n
```

Expected: FAIL — `RegisterCustomerCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Portal/Auth/Commands/RegisterCustomerCommand.cs
using System.Security.Cryptography;
using System.Text;
using CRM.Application.Common;
using CRM.Domain.Customers;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace CRM.Application.Portal.Auth.Commands;

public record RegisterCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest;

public class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerCredentialRepository _credentials;
    private readonly IEmailVerificationTokenRepository _tokens;
    private readonly IEmailService _email;
    private readonly string _frontendUrl;

    public RegisterCustomerCommandHandler(
        ICustomerRepository customers,
        ICustomerCredentialRepository credentials,
        IEmailVerificationTokenRepository tokens,
        IEmailService email,
        IConfiguration? config = null)
    {
        _customers = customers;
        _credentials = credentials;
        _tokens = tokens;
        _email = email;
        _frontendUrl = config?["App:PortalUrl"] ?? "https://portal.crm.local";
    }

    public async Task Handle(RegisterCustomerCommand cmd, CancellationToken ct)
    {
        var existing = await _customers.FindByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Email '{cmd.Email}' is already registered.");

        var customer = Customer.Create(cmd.FirstName, cmd.LastName, cmd.Email);
        await _customers.AddAsync(customer, ct);

        var credential = CustomerCredential.Create(
            customer.Id,
            BCrypt.Net.BCrypt.HashPassword(cmd.Password));
        await _credentials.AddAsync(credential, ct);

        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var token = EmailVerificationToken.Create(customer.Id, hash, DateTime.UtcNow.AddHours(24));
        await _tokens.AddAsync(token, ct);

        await _customers.SaveChangesAsync(ct);

        var link = $"{_frontendUrl}/auth/verify-email?token={Uri.EscapeDataString(raw)}";
        await _email.SendEmailVerificationAsync(
            customer.Email, $"{customer.FirstName} {customer.LastName}", link, ct);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Portal/Auth/Validators/RegisterCustomerCommandValidator.cs
using CRM.Application.Portal.Auth.Commands;
using FluentValidation;

namespace CRM.Application.Portal.Auth.Validators;

public class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Must contain a lowercase letter.")
            .Matches(@"\d").WithMessage("Must contain a digit.")
            .Matches(@"[^a-zA-Z\d]").WithMessage("Must contain a special character.");
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RegisterCustomerCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Portal/Auth/ \
        tests/CRM.Application.Tests/Portal/Auth/RegisterCustomerCommandHandlerTests.cs
git commit -m "feat(portal): add RegisterCustomerCommand with email verification dispatch"
```

---

## Task 3: PortalAuthController — POST /api/portal/auth/register

**Files:**
- Create: `src/CRM.API/Controllers/Portal/PortalAuthController.cs`
- Test: `tests/CRM.API.Tests/Portal/PortalAuthControllerRegisterTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Portal/PortalAuthControllerRegisterTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalAuthControllerRegisterTests
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
    public async Task Register_ValidBody_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/portal/auth/register",
            new { firstName = "Ali", lastName = "Nasser", email = "ali@portal.test",
                  password = "P@ssword1!" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Email already registered."));
        var client = BuildClient();

        var response = await client.PostAsJsonAsync("/api/portal/auth/register",
            new { firstName = "Ali", lastName = "Nasser", email = "dup@portal.test",
                  password = "P@ssword1!" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalAuthControllerRegisterTests" -v n
```

Expected: FAIL — `PortalAuthController` does not exist yet.

- [ ] **Step 3: Implement PortalAuthController**

```csharp
// src/CRM.API/Controllers/Portal/PortalAuthController.cs
using CRM.Application.Portal.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers.Portal;

[ApiController]
[Route("api/portal/auth")]
public class PortalAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public PortalAuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCustomerCommand command, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(command, ct);
            return StatusCode(201, new
            {
                message = "Registration successful. Please check your email to verify your account."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalAuthControllerRegisterTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/Portal/PortalAuthController.cs \
        tests/CRM.API.Tests/Portal/PortalAuthControllerRegisterTests.cs
git commit -m "feat(api): add POST /api/portal/auth/register endpoint"
```
