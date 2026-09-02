# Add Customer Contact — Implementation Plan

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

**Story:** US-BE-017  
**Goal:** Implement `POST /api/customers/{id}/contacts` — adds a contact entry (Phone, Email, WhatsApp) to a customer.

**Architecture:** `AddCustomerContactCommand(customerId, type, value, isPrimary)` → handler fetches customer with contacts, calls `customer.AddContact(...)`, saves. If `isPrimary = true` and another contact of the same type is already primary, the existing one is demoted. Returns the updated contact list.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Commands/AddCustomerContactCommand.cs` |
| Create | `src/CRM.Application/Customers/Validators/AddCustomerContactCommandValidator.cs` |
| Modify | `src/CRM.Domain/Customers/Customer.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/AddCustomerContactCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerContactTests.cs` |

---

## Task 1: Update Customer.AddContact to Handle Primary Demotion

**Files:**
- Modify: `src/CRM.Domain/Customers/Customer.cs`

- [ ] **Step 1: Update Customer domain to support primary demotion**

Replace the existing `AddContact` method in `src/CRM.Domain/Customers/Customer.cs`:

```csharp
// Replace AddContact method in Customer class:
public void AddContact(string type, string value, bool isPrimary)
{
    if (isPrimary)
    {
        // Demote existing primary contact of same type
        foreach (var c in _contacts.Where(c => c.Type == type && c.IsPrimary))
            c.DemotePrimary();
    }
    _contacts.Add(CustomerContact.Create(Id, type, value, isPrimary));
    UpdatedAt = DateTime.UtcNow;
}
```

Also add `DemotePrimary()` to `CustomerContact`:

```csharp
// Add to CustomerContact class in src/CRM.Domain/Customers/CustomerContact.cs:
public void DemotePrimary() => IsPrimary = false;
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Customers/Customer.cs \
        src/CRM.Domain/Customers/CustomerContact.cs
git commit -m "feat(domain): add primary demotion logic to Customer.AddContact"
```

---

## Task 2: AddCustomerContact Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Customers/Commands/AddCustomerContactCommand.cs`
- Create: `src/CRM.Application/Customers/Validators/AddCustomerContactCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Customers/AddCustomerContactCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/AddCustomerContactCommandHandlerTests.cs
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class AddCustomerContactCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly AddCustomerContactCommandHandler _handler;

    public AddCustomerContactCommandHandlerTests()
    {
        _handler = new AddCustomerContactCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ValidContact_AddsContactToCustomer()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(
            new AddCustomerContactCommand(id, "Phone", "+971501234567", true), default);

        Assert.Single(result);
        Assert.Equal("Phone", result[0].Type);
        Assert.Equal("+971501234567", result[0].Value);
        Assert.True(result[0].IsPrimary);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_AddPrimaryWhenAlreadyHasPrimary_DemotesOldPrimary()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.AddContact("Phone", "+971501111111", true);

        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(
            new AddCustomerContactCommand(id, "Phone", "+971502222222", true), default);

        Assert.Equal(2, result.Count);
        var oldContact = result.First(c => c.Value == "+971501111111");
        var newContact = result.First(c => c.Value == "+971502222222");
        Assert.False(oldContact.IsPrimary);
        Assert.True(newContact.IsPrimary);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new AddCustomerContactCommand(Guid.NewGuid(), "Phone", "+971500000000", false),
                default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AddCustomerContactCommandHandlerTests" -v n
```

Expected: FAIL — `AddCustomerContactCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Customers/Commands/AddCustomerContactCommand.cs
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record AddCustomerContactCommand(
    Guid CustomerId,
    string Type,
    string Value,
    bool IsPrimary) : IRequest<IReadOnlyList<ContactDto>>;

public class AddCustomerContactCommandHandler
    : IRequestHandler<AddCustomerContactCommand, IReadOnlyList<ContactDto>>
{
    private readonly ICustomerRepository _customers;

    public AddCustomerContactCommandHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<IReadOnlyList<ContactDto>> Handle(
        AddCustomerContactCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdWithContactsAsync(cmd.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.AddContact(cmd.Type, cmd.Value, cmd.IsPrimary);
        await _customers.SaveChangesAsync(ct);

        return customer.Contacts
            .Select(c => new ContactDto(c.Id, c.Type, c.Value, c.IsPrimary))
            .ToList();
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Customers/Validators/AddCustomerContactCommandValidator.cs
using CRM.Application.Customers.Commands;
using FluentValidation;

namespace CRM.Application.Customers.Validators;

public class AddCustomerContactCommandValidator : AbstractValidator<AddCustomerContactCommand>
{
    private static readonly string[] AllowedTypes = { "Phone", "Email", "WhatsApp" };

    public AddCustomerContactCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t))
            .WithMessage("Type must be Phone, Email, or WhatsApp.");
        RuleFor(x => x.Value).NotEmpty().MaximumLength(256);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AddCustomerContactCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Customers/Commands/AddCustomerContactCommand.cs \
        src/CRM.Application/Customers/Validators/AddCustomerContactCommandValidator.cs \
        tests/CRM.Application.Tests/Customers/AddCustomerContactCommandHandlerTests.cs
git commit -m "feat(customers): add AddCustomerContactCommand with primary demotion"
```

---

## Task 3: CustomersController — POST /api/customers/{id}/contacts

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerContactTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerContactTests.cs
using System.Collections.Generic;
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

public class CustomersControllerContactTests
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
    public async Task AddContact_ValidBody_Returns200WithContacts()
    {
        var contacts = new List<ContactDto>
        {
            new(Guid.NewGuid(), "Phone", "+971501234567", true)
        };
        _mediator.Setup(m => m.Send(It.IsAny<AddCustomerContactCommand>(), default))
                 .ReturnsAsync((IReadOnlyList<ContactDto>)contacts);

        var client = BuildClient();
        var response = await client.PostAsJsonAsync($"/api/customers/{Guid.NewGuid()}/contacts",
            new { type = "Phone", value = "+971501234567", isPrimary = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerContactTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add AddContact endpoint to CustomersController**

```csharp
// Add to src/CRM.API/Controllers/CustomersController.cs inside the class:

public record AddContactRequest(string Type, string Value, bool IsPrimary);

[HttpPost("{id:guid}/contacts")]
public async Task<IActionResult> AddContact(
    Guid id, [FromBody] AddContactRequest request, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new AddCustomerContactCommand(id, request.Type, request.Value, request.IsPrimary), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerContactTests" -v n
```

Expected: 1 test PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerContactTests.cs
git commit -m "feat(api): add POST /api/customers/{id}/contacts endpoint"
```
