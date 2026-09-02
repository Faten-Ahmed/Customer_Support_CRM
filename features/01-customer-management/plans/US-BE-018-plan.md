# Remove Customer Contact — Implementation Plan

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

**Story:** US-BE-018  
**Goal:** Implement `DELETE /api/customers/{customerId}/contacts/{contactId}` — removes a contact entry from a customer. Cannot remove the last primary contact if it's the only contact of that type.

**Architecture:** `RemoveCustomerContactCommand(customerId, contactId)` → handler fetches customer with contacts, locates contact by ID, validates it's not the sole primary contact for its type (to avoid orphaning), calls `customer.RemoveContact(contactId)`, saves.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Commands/RemoveCustomerContactCommand.cs` |
| Modify | `src/CRM.Domain/Customers/Customer.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/RemoveCustomerContactCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerRemoveContactTests.cs` |

---

## Task 1: Update Customer Domain for RemoveContact

**Files:**
- Modify: `src/CRM.Domain/Customers/Customer.cs`

- [ ] **Step 1: Add RemoveContact method to Customer**

```csharp
// Add to Customer class in src/CRM.Domain/Customers/Customer.cs:
public void RemoveContact(Guid contactId)
{
    var contact = _contacts.FirstOrDefault(c => c.Id == contactId)
        ?? throw new KeyNotFoundException($"Contact {contactId} not found.");

    _contacts.Remove(contact);
    UpdatedAt = DateTime.UtcNow;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Customers/Customer.cs
git commit -m "feat(domain): add Customer.RemoveContact method"
```

---

## Task 2: RemoveCustomerContact Command + Handler

**Files:**
- Create: `src/CRM.Application/Customers/Commands/RemoveCustomerContactCommand.cs`
- Test: `tests/CRM.Application.Tests/Customers/RemoveCustomerContactCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/RemoveCustomerContactCommandHandlerTests.cs
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class RemoveCustomerContactCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly RemoveCustomerContactCommandHandler _handler;

    public RemoveCustomerContactCommandHandlerTests()
    {
        _handler = new RemoveCustomerContactCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingContact_RemovesIt()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.AddContact("Phone", "+971501234567", true);
        customer.AddContact("Phone", "+971502345678", false);
        var contactToRemove = customer.Contacts[1];

        _repo.Setup(r => r.FindByIdWithContactsAsync(customerId, default)).ReturnsAsync(customer);

        await _handler.Handle(
            new RemoveCustomerContactCommand(customerId, contactToRemove.Id), default);

        Assert.Single(customer.Contacts);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_RemoveSolePrimaryPhone_ThrowsInvalidOperationException()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.AddContact("Phone", "+971501234567", true);
        var contact = customer.Contacts[0];

        _repo.Setup(r => r.FindByIdWithContactsAsync(customerId, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new RemoveCustomerContactCommand(customerId, contact.Id), default));
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new RemoveCustomerContactCommand(Guid.NewGuid(), Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_NonExistentContact_ThrowsKeyNotFoundException()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        _repo.Setup(r => r.FindByIdWithContactsAsync(customerId, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new RemoveCustomerContactCommand(customerId, Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RemoveCustomerContactCommandHandlerTests" -v n
```

Expected: FAIL — `RemoveCustomerContactCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Customers/Commands/RemoveCustomerContactCommand.cs
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record RemoveCustomerContactCommand(Guid CustomerId, Guid ContactId) : IRequest;

public class RemoveCustomerContactCommandHandler : IRequestHandler<RemoveCustomerContactCommand>
{
    private readonly ICustomerRepository _customers;

    public RemoveCustomerContactCommandHandler(ICustomerRepository customers)
        => _customers = customers;

    public async Task Handle(RemoveCustomerContactCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdWithContactsAsync(cmd.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        var contact = customer.Contacts.FirstOrDefault(c => c.Id == cmd.ContactId)
            ?? throw new KeyNotFoundException($"Contact {cmd.ContactId} not found.");

        // Guard: cannot remove the only primary contact of its type
        if (contact.IsPrimary)
        {
            var sameType = customer.Contacts.Where(c => c.Type == contact.Type).ToList();
            if (sameType.Count == 1)
                throw new InvalidOperationException(
                    $"Cannot remove the only {contact.Type} contact. Add another {contact.Type} contact first.");
        }

        customer.RemoveContact(cmd.ContactId);
        await _customers.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RemoveCustomerContactCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Customers/Commands/RemoveCustomerContactCommand.cs \
        tests/CRM.Application.Tests/Customers/RemoveCustomerContactCommandHandlerTests.cs
git commit -m "feat(customers): add RemoveCustomerContactCommand with sole-primary guard"
```

---

## Task 3: CustomersController — DELETE /api/customers/{id}/contacts/{contactId}

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerRemoveContactTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerRemoveContactTests.cs
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerRemoveContactTests
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
    public async Task RemoveContact_ExistingContact_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.DeleteAsync(
            $"/api/customers/{Guid.NewGuid()}/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveContact_SolePrimary_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Cannot remove sole primary."));
        var client = BuildClient();

        var response = await client.DeleteAsync(
            $"/api/customers/{Guid.NewGuid()}/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerRemoveContactTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add RemoveContact endpoint to CustomersController**

```csharp
// Add to src/CRM.API/Controllers/CustomersController.cs inside the class:

[HttpDelete("{id:guid}/contacts/{contactId:guid}")]
public async Task<IActionResult> RemoveContact(
    Guid id, Guid contactId, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new RemoveCustomerContactCommand(id, contactId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerRemoveContactTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerRemoveContactTests.cs
git commit -m "feat(api): add DELETE /api/customers/{id}/contacts/{contactId} endpoint"
```
