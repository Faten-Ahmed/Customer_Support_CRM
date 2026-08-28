# Update Customer — Implementation Plan

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

**Story:** US-BE-014  
**Goal:** Implement `PUT /api/customers/{id}` — updates a customer's name and phone (email is immutable); returns the updated `CustomerDetailDto`.

**Architecture:** `UpdateCustomerCommand(id, firstName, lastName, phone)` → handler fetches customer, calls `customer.Update(...)`, saves. Email cannot be changed — validator enforces no email in body. Returns 404 if not found or deleted.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Commands/UpdateCustomerCommand.cs` |
| Create | `src/CRM.Application/Customers/Validators/UpdateCustomerCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/UpdateCustomerCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerUpdateTests.cs` |

---

## Task 1: UpdateCustomer Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Customers/Commands/UpdateCustomerCommand.cs`
- Create: `src/CRM.Application/Customers/Validators/UpdateCustomerCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Customers/UpdateCustomerCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/UpdateCustomerCommandHandlerTests.cs
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class UpdateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly UpdateCustomerCommandHandler _handler;

    public UpdateCustomerCommandHandlerTests()
    {
        _handler = new UpdateCustomerCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_UpdatesNameAndPhone()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test", "+971501111111");
        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(
            new UpdateCustomerCommand(id, "Ahmed", "Al-Rashid", "+971509999999"), default);

        Assert.Equal("Ahmed", result.FirstName);
        Assert.Equal("Al-Rashid", result.LastName);
        Assert.Equal("+971509999999", result.Phone);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new UpdateCustomerCommand(Guid.NewGuid(), "X", "Y", null), default));
    }

    [Fact]
    public async Task Handle_DeletedCustomer_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.SoftDelete();

        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new UpdateCustomerCommand(id, "X", "Y", null), default));
    }

    [Fact]
    public async Task Handle_NullPhone_ClearsPhone()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test", "+971501111111");
        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(
            new UpdateCustomerCommand(id, "Ali", "Hassan", null), default);

        Assert.Null(result.Phone);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UpdateCustomerCommandHandlerTests" -v n
```

Expected: FAIL — `UpdateCustomerCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Customers/Commands/UpdateCustomerCommand.cs
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record UpdateCustomerCommand(
    Guid CustomerId,
    string FirstName,
    string LastName,
    string? Phone) : IRequest<CustomerDetailDto>;

public class UpdateCustomerCommandHandler
    : IRequestHandler<UpdateCustomerCommand, CustomerDetailDto>
{
    private readonly ICustomerRepository _customers;

    public UpdateCustomerCommandHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<CustomerDetailDto> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdWithContactsAsync(cmd.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.Update(cmd.FirstName, cmd.LastName, cmd.Phone);
        await _customers.SaveChangesAsync(ct);

        return new CustomerDetailDto(
            customer.Id, customer.FirstName, customer.LastName,
            customer.Email, customer.Phone, customer.IsVip, customer.IsDeleted,
            customer.CreatedAt, customer.UpdatedAt,
            customer.Contacts.Select(c => new ContactDto(c.Id, c.Type, c.Value, c.IsPrimary)).ToList());
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Customers/Validators/UpdateCustomerCommandValidator.cs
using CRM.Application.Customers.Commands;
using FluentValidation;

namespace CRM.Application.Customers.Validators;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UpdateCustomerCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Customers/Commands/UpdateCustomerCommand.cs \
        src/CRM.Application/Customers/Validators/UpdateCustomerCommandValidator.cs \
        tests/CRM.Application.Tests/Customers/UpdateCustomerCommandHandlerTests.cs
git commit -m "feat(customers): add UpdateCustomerCommand"
```

---

## Task 2: CustomersController — PUT /api/customers/{id}

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerUpdateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerUpdateTests.cs
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

public class CustomersControllerUpdateTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Manager"));
        return client;
    }

    [Fact]
    public async Task Update_ExistingCustomer_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateCustomerCommand>(), default))
                 .ReturnsAsync(new CustomerDetailDto(
                     id, "Ahmed", "Al-Rashid", "ali@crm.test", "+971509999999",
                     false, false, DateTime.UtcNow, DateTime.UtcNow, new List<ContactDto>()));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/customers/{id}",
            new { firstName = "Ahmed", lastName = "Al-Rashid", phone = "+971509999999" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentCustomer_Returns404()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UpdateCustomerCommand>(), default))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));

        var client = BuildClient();
        var response = await client.PutAsJsonAsync($"/api/customers/{id}",
            new { firstName = "X", lastName = "Y", phone = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerUpdateTests" -v n
```

Expected: FAIL — `PUT /api/customers/{id}` does not exist yet.

- [ ] **Step 3: Add Update endpoint to CustomersController**

```csharp
// Add to src/CRM.API/Controllers/CustomersController.cs inside the class:

public record UpdateCustomerRequest(string FirstName, string LastName, string? Phone);

[HttpPut("{id:guid}")]
public async Task<IActionResult> Update(
    Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new UpdateCustomerCommand(id, request.FirstName, request.LastName, request.Phone), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerUpdateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerUpdateTests.cs
git commit -m "feat(api): add PUT /api/customers/{id} endpoint"
```
