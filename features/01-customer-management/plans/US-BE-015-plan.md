# Soft Delete Customer — Implementation Plan

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

**Story:** US-BE-015  
**Goal:** Implement `DELETE /api/customers/{id}` — soft-deletes a customer (sets `IsDeleted = true`) so they disappear from listings but historical tickets are preserved. Admin only.

**Architecture:** `DeleteCustomerCommand(id)` → handler fetches customer, calls `customer.SoftDelete()`, saves. Returns 409 if the customer has open tickets (state New/Assigned/InProgress/OnHold/Escalated). Returns 404 if already deleted.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Commands/DeleteCustomerCommand.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/DeleteCustomerCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerDeleteTests.cs` |

---

## Task 1: DeleteCustomer Command + Handler

**Files:**
- Create: `src/CRM.Application/Customers/Commands/DeleteCustomerCommand.cs`
- Test: `tests/CRM.Application.Tests/Customers/DeleteCustomerCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/DeleteCustomerCommandHandlerTests.cs
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class DeleteCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly DeleteCustomerCommandHandler _handler;

    public DeleteCustomerCommandHandlerTests()
    {
        _handler = new DeleteCustomerCommandHandler(
            _customerRepo.Object, _ticketRepo.Object);
    }

    [Fact]
    public async Task Handle_CustomerWithNoOpenTickets_SoftDeletes()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        _customerRepo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);
        _ticketRepo.Setup(r => r.HasOpenTicketsAsync(id, default)).ReturnsAsync(false);

        await _handler.Handle(new DeleteCustomerCommand(id), default);

        Assert.True(customer.IsDeleted);
        _customerRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_CustomerWithOpenTickets_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        _customerRepo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);
        _ticketRepo.Setup(r => r.HasOpenTicketsAsync(id, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new DeleteCustomerCommand(id), default));

        Assert.False(customer.IsDeleted);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new DeleteCustomerCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_AlreadyDeletedCustomer_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.SoftDelete();
        _customerRepo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new DeleteCustomerCommand(id), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeleteCustomerCommandHandlerTests" -v n
```

Expected: FAIL — `DeleteCustomerCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Customers/Commands/DeleteCustomerCommand.cs
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record DeleteCustomerCommand(Guid CustomerId) : IRequest;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customers, ITicketRepository tickets)
    {
        _customers = customers;
        _tickets = tickets;
    }

    public async Task Handle(DeleteCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        var hasOpen = await _tickets.HasOpenTicketsAsync(cmd.CustomerId, ct);
        if (hasOpen)
            throw new InvalidOperationException(
                "Cannot delete a customer with open tickets. Resolve or close all tickets first.");

        customer.SoftDelete();
        await _customers.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeleteCustomerCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Customers/Commands/DeleteCustomerCommand.cs \
        tests/CRM.Application.Tests/Customers/DeleteCustomerCommandHandlerTests.cs
git commit -m "feat(customers): add DeleteCustomerCommand with open-ticket guard"
```

---

## Task 2: CustomersController — DELETE /api/customers/{id}

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerDeleteTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerDeleteTests.cs
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerDeleteTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Admin"));
        return client;
    }

    [Fact]
    public async Task Delete_ExistingCustomerNoOpenTickets_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.DeleteAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CustomerWithOpenTickets_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new InvalidOperationException("Open tickets exist."));
        var client = BuildClient();

        var response = await client.DeleteAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotAdmin_Returns403()
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

        var response = await client.DeleteAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerDeleteTests" -v n
```

Expected: FAIL — `DELETE /api/customers/{id}` does not exist yet.

- [ ] **Step 3: Add Delete endpoint to CustomersController**

```csharp
// Add to src/CRM.API/Controllers/CustomersController.cs inside the class:

[Authorize(Roles = "Admin")]
[HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new DeleteCustomerCommand(id), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerDeleteTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerDeleteTests.cs
git commit -m "feat(api): add DELETE /api/customers/{id} (Admin only, soft delete)"
```
