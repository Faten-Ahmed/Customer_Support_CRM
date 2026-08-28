# Create Customer (Internal) — Implementation Plan

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

**Story:** US-BE-009  
**Goal:** Implement `POST /api/customers` — allows Admin/Manager/Agent to create a customer record with name, email, phone, and optional VIP flag.

**Architecture:** `CreateCustomerCommand` → handler checks email uniqueness, creates `Customer` aggregate, persists via `ICustomerRepository`. Returns `CustomerDto` with the new ID. Endpoint protected by `[Authorize(Roles = "Admin,Manager,Agent")]`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Customers/Customer.cs` |
| Create | `src/CRM.Domain/Customers/CustomerContact.cs` |
| Create | `src/CRM.Domain/Customers/ICustomerRepository.cs` |
| Create | `src/CRM.Application/Customers/Commands/CreateCustomerCommand.cs` |
| Create | `src/CRM.Application/Customers/DTOs/CustomerDto.cs` |
| Create | `src/CRM.Application/Customers/Validators/CreateCustomerCommandValidator.cs` |
| Create | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/CreateCustomerCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerCreateTests.cs` |

---

## Task 1: Customer Domain Aggregate

**Files:**
- Create: `src/CRM.Domain/Customers/Customer.cs`
- Create: `src/CRM.Domain/Customers/CustomerContact.cs`
- Create: `src/CRM.Domain/Customers/ICustomerRepository.cs`

- [ ] **Step 1: Create Customer aggregate and Contact value object**

```csharp
// src/CRM.Domain/Customers/CustomerContact.cs
namespace CRM.Domain.Customers;

public class CustomerContact
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Type { get; private set; } = null!;  // Phone, Email, WhatsApp
    public string Value { get; private set; } = null!;
    public bool IsPrimary { get; private set; }

    private CustomerContact() { }

    public static CustomerContact Create(Guid customerId, string type, string value, bool isPrimary)
        => new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = type,
            Value = value,
            IsPrimary = isPrimary
        };
}
```

```csharp
// src/CRM.Domain/Customers/Customer.cs
namespace CRM.Domain.Customers;

public class Customer
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public bool IsVip { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<CustomerContact> _contacts = new();
    public IReadOnlyList<CustomerContact> Contacts => _contacts.AsReadOnly();

    private Customer() { }

    public static Customer Create(string firstName, string lastName, string email,
        string? phone = null, bool isVip = false)
        => new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            Phone = phone,
            IsVip = isVip,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string firstName, string lastName, string? phone)
    {
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVip(bool isVip)
    {
        IsVip = isVip;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddContact(string type, string value, bool isPrimary)
        => _contacts.Add(CustomerContact.Create(Id, type, value, isPrimary));
}
```

```csharp
// src/CRM.Domain/Customers/ICustomerRepository.cs
namespace CRM.Domain.Customers;

public interface ICustomerRepository
{
    Task<Customer?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Customer?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Customer customer, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Customers/
git commit -m "feat(domain): add Customer aggregate, CustomerContact, and ICustomerRepository"
```

---

## Task 2: CreateCustomer Command + Handler + Validator + DTO

**Files:**
- Create: `src/CRM.Application/Customers/DTOs/CustomerDto.cs`
- Create: `src/CRM.Application/Customers/Commands/CreateCustomerCommand.cs`
- Create: `src/CRM.Application/Customers/Validators/CreateCustomerCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Customers/CreateCustomerCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/CreateCustomerCommandHandlerTests.cs
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _handler = new CreateCustomerCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesCustomerAndReturnsDto()
    {
        _repo.Setup(r => r.FindByEmailAsync("john@example.com", default))
             .ReturnsAsync((Customer?)null);

        var result = await _handler.Handle(
            new CreateCustomerCommand("John", "Doe", "john@example.com", "+971501234567", false),
            default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        _repo.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        var existing = Customer.Create("Jane", "Smith", "jane@example.com");
        _repo.Setup(r => r.FindByEmailAsync("jane@example.com", default)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateCustomerCommand("Jane", "Other", "jane@example.com", null, false),
                default));
    }

    [Fact]
    public async Task Handle_VipFlag_SetsIsVip()
    {
        _repo.Setup(r => r.FindByEmailAsync("vip@example.com", default))
             .ReturnsAsync((Customer?)null);

        Customer? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Customer>(), default))
             .Callback<Customer, CancellationToken>((c, _) => captured = c)
             .Returns(Task.CompletedTask);

        await _handler.Handle(
            new CreateCustomerCommand("Big", "Client", "vip@example.com", null, true),
            default);

        Assert.NotNull(captured);
        Assert.True(captured!.IsVip);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateCustomerCommandHandlerTests" -v n
```

Expected: FAIL — `CreateCustomerCommand` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Customers/DTOs/CustomerDto.cs
namespace CRM.Application.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    bool IsVip,
    DateTime CreatedAt);
```

- [ ] **Step 4: Create command and handler**

```csharp
// src/CRM.Application/Customers/Commands/CreateCustomerCommand.cs
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    bool IsVip) : IRequest<CustomerDto>;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customers;

    public CreateCustomerCommandHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<CustomerDto> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var existing = await _customers.FindByEmailAsync(cmd.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A customer with email '{cmd.Email}' already exists.");

        var customer = Customer.Create(cmd.FirstName, cmd.LastName, cmd.Email, cmd.Phone, cmd.IsVip);
        await _customers.AddAsync(customer, ct);
        await _customers.SaveChangesAsync(ct);

        return new CustomerDto(
            customer.Id, customer.FirstName, customer.LastName,
            customer.Email, customer.Phone, customer.IsVip, customer.CreatedAt);
    }
}
```

- [ ] **Step 5: Create validator**

```csharp
// src/CRM.Application/Customers/Validators/CreateCustomerCommandValidator.cs
using CRM.Application.Customers.Commands;
using FluentValidation;

namespace CRM.Application.Customers.Validators;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateCustomerCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Customers/ \
        tests/CRM.Application.Tests/Customers/CreateCustomerCommandHandlerTests.cs
git commit -m "feat(customers): add CreateCustomerCommand with duplicate email guard"
```

---

## Task 3: CustomersController — POST /api/customers

**Files:**
- Create: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerCreateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerCreateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Customers.Commands;
using CRM.Application.Customers.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerCreateTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task CreateCustomer_ValidBody_Returns201WithLocation()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateCustomerCommand>(), default))
                 .ReturnsAsync(new CustomerDto(id, "John", "Doe", "john@example.com",
                     "+971501234567", false, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/customers",
            new { firstName = "John", lastName = "Doe", email = "john@example.com",
                  phone = "+971501234567", isVip = false });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains($"/api/customers/{id}", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateCustomerCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Email already exists."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/customers",
            new { firstName = "Jane", lastName = "Doe", email = "dup@example.com",
                  phone = (string?)null, isVip = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Unauthenticated_Returns401()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/customers",
            new { firstName = "X", lastName = "Y", email = "x@y.com", isVip = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerCreateTests" -v n
```

Expected: FAIL — `CustomersController` does not exist yet.

- [ ] **Step 3: Implement CustomersController**

```csharp
// src/CRM.API/Controllers/CustomersController.cs
using CRM.Application.Customers.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CustomersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => Ok(); // Implemented in US-BE-012
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerCreateTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerCreateTests.cs
git commit -m "feat(api): add POST /api/customers endpoint"
```
