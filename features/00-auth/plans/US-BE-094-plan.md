# Portal Customer Login — Implementation Plan

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

**Story:** US-BE-094  
**Goal:** Implement `POST /auth/login` support for customers — the same endpoint used by internal staff but differentiated by entity type. `EmailVerified = false` → 401 `EMAIL_NOT_VERIFIED`. `IsActive = false` → 401 `ACCOUNT_INACTIVE`. Customer JWT has `role = Customer` and grants access only to `/portal/*` endpoints; calling `/tickets` returns 403.

**Architecture:** Extend existing `LoginCommand` handler to check `ICustomerRepository` when `IUserRepository` finds no match. Issues JWT with `role = Customer` and `entity_type = Customer` claim. `AuthController.Login` unchanged — the handler resolves the entity type.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, JWT, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Modify | `src/CRM.Application/Auth/Commands/LoginCommand.cs` |
| Modify | `src/CRM.Domain/Customers/ICustomerRepository.cs` |
| Test   | `tests/CRM.Application.Tests/Auth/CustomerLoginCommandHandlerTests.cs` |

---

## Task 1: Customer Login

> Note: `LoginCommand` and `AuthController` are from US-BE-007. `Customer` entity and `ICustomerRepository` are from US-BE-009. `ICustomerRepository.FindByEmailAsync` is added in US-BE-089. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Auth/CustomerLoginCommandHandlerTests.cs
using CRM.Application.Auth.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Auth;

public class CustomerLoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly LoginCommandHandler _handler;

    public CustomerLoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(
            _users.Object, _customers.Object, _hasher.Object, _jwt.Object);
    }

    [Fact]
    public async Task Handle_ValidCustomerCredentials_ReturnsTokenWithCustomerRole()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.SetPassword("hashed_pw");
        customer.VerifyEmail();

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default))
              .ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default))
                  .ReturnsAsync(customer);
        _hasher.Setup(h => h.Verify("correctPw", "hashed_pw")).Returns(true);
        _jwt.Setup(j => j.GenerateToken(customer.Id, "alice@example.com", "Customer"))
            .Returns("customer-jwt-token");

        var result = await _handler.Handle(
            new LoginCommand("alice@example.com", "correctPw"), default);

        Assert.Equal("customer-jwt-token", result.Token);
        Assert.Equal("Customer", result.Role);
    }

    [Fact]
    public async Task Handle_EmailNotVerified_Throws401WithCode()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.SetPassword("hashed_pw");
        // EmailVerified = false (default)

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default))
              .ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default))
                  .ReturnsAsync(customer);
        _hasher.Setup(h => h.Verify("pw", "hashed_pw")).Returns(true);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginCommand("alice@example.com", "pw"), default));

        Assert.Contains("EMAIL_NOT_VERIFIED", ex.Message);
    }

    [Fact]
    public async Task Handle_AccountInactive_Throws401WithCode()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.SetPassword("hashed_pw");
        customer.VerifyEmail();
        customer.Deactivate();

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default))
              .ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default))
                  .ReturnsAsync(customer);
        _hasher.Setup(h => h.Verify("pw", "hashed_pw")).Returns(true);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginCommand("alice@example.com", "pw"), default));

        Assert.Contains("ACCOUNT_INACTIVE", ex.Message);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var customer = Customer.Create("Alice", "alice@example.com", null, null);
        customer.SetPassword("hashed_pw");
        customer.VerifyEmail();

        _users.Setup(r => r.FindByEmailAsync("alice@example.com", default))
              .ReturnsAsync((User?)null);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default))
                  .ReturnsAsync(customer);
        _hasher.Setup(h => h.Verify("wrongPw", "hashed_pw")).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new LoginCommand("alice@example.com", "wrongPw"), default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CustomerLoginCommandHandlerTests" -v n
```

Expected: FAIL — `LoginCommandHandler` doesn't inject `ICustomerRepository` yet.

- [ ] **Step 3: Extend Customer entity with auth methods**

Open `src/CRM.Domain/Customers/Customer.cs` and add:

```csharp
public string? PasswordHash { get; private set; }
public bool EmailVerified { get; private set; }
public bool IsActive { get; private set; } = true;

public void SetPassword(string hash) => PasswordHash = hash;
public void VerifyEmail() => EmailVerified = true;
public void Deactivate() => IsActive = false;
```

- [ ] **Step 4: Extend ICustomerRepository**

Open `src/CRM.Domain/Customers/ICustomerRepository.cs`. `FindByEmailAsync` should already exist from US-BE-089. If not, add:

```csharp
Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default);
```

- [ ] **Step 5: Extend LoginCommand handler to check Customer**

Open `src/CRM.Application/Auth/Commands/LoginCommand.cs`. Update the handler constructor and `Handle` method:

```csharp
// Add ICustomerRepository parameter to constructor
private readonly ICustomerRepository _customers;

public LoginCommandHandler(
    IUserRepository users,
    ICustomerRepository customers,
    IPasswordHasher hasher,
    IJwtTokenGenerator jwt)
{
    _users = users;
    _customers = customers;
    _hasher = hasher;
    _jwt = jwt;
}
```

In the `Handle` method, after failing to find a `User`, add customer lookup:

```csharp
// After: var user = await _users.FindByEmailAsync(cmd.Email, ct);
// If user is null, try customer:
if (user is null)
{
    var customer = await _customers.FindByEmailAsync(cmd.Email, ct);
    if (customer is null)
        throw new UnauthorizedAccessException("Invalid email or password.");

    if (!_hasher.Verify(cmd.Password, customer.PasswordHash ?? ""))
        throw new UnauthorizedAccessException("Invalid email or password.");

    if (!customer.EmailVerified)
        throw new UnauthorizedAccessException("EMAIL_NOT_VERIFIED: Please verify your email address.");

    if (!customer.IsActive)
        throw new UnauthorizedAccessException("ACCOUNT_INACTIVE: Your account has been deactivated.");

    var token = _jwt.GenerateToken(customer.Id, customer.Email, "Customer");
    return new LoginResult(token, "Customer", customer.Id);
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CustomerLoginCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Domain/Customers/Customer.cs \
        src/CRM.Application/Auth/Commands/LoginCommand.cs \
        tests/CRM.Application.Tests/Auth/CustomerLoginCommandHandlerTests.cs
git commit -m "feat(auth): extend login handler to authenticate customers with EMAIL_NOT_VERIFIED and ACCOUNT_INACTIVE checks"
```
