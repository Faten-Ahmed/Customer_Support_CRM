# Flag VIP — Implementation Plan

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

**Story:** US-BE-016  
**Goal:** Implement `PATCH /api/customers/{id}/vip` — toggles the VIP flag on a customer record. Admin/Manager only.

**Architecture:** `SetCustomerVipCommand(id, isVip)` → handler fetches customer, calls `customer.SetVip(isVip)`, saves. Returns 204. Simple idempotent operation — setting VIP on an already-VIP customer is a no-op.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Commands/SetCustomerVipCommand.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/SetCustomerVipCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerVipTests.cs` |

---

## Task 1: SetCustomerVip Command + Handler

**Files:**
- Create: `src/CRM.Application/Customers/Commands/SetCustomerVipCommand.cs`
- Test: `tests/CRM.Application.Tests/Customers/SetCustomerVipCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/SetCustomerVipCommandHandlerTests.cs
using CRM.Application.Customers.Commands;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class SetCustomerVipCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly SetCustomerVipCommandHandler _handler;

    public SetCustomerVipCommandHandlerTests()
    {
        _handler = new SetCustomerVipCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_SetVipTrue_SetsIsVipTrue()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        Assert.False(customer.IsVip);

        _repo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);

        await _handler.Handle(new SetCustomerVipCommand(id, true), default);

        Assert.True(customer.IsVip);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_SetVipFalse_SetsIsVipFalse()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test", null, true);
        Assert.True(customer.IsVip);

        _repo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);

        await _handler.Handle(new SetCustomerVipCommand(id, false), default);

        Assert.False(customer.IsVip);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new SetCustomerVipCommand(Guid.NewGuid(), true), default));
    }

    [Fact]
    public async Task Handle_DeletedCustomer_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.SoftDelete();

        _repo.Setup(r => r.FindByIdAsync(id, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new SetCustomerVipCommand(id, true), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SetCustomerVipCommandHandlerTests" -v n
```

Expected: FAIL — `SetCustomerVipCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Customers/Commands/SetCustomerVipCommand.cs
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Commands;

public record SetCustomerVipCommand(Guid CustomerId, bool IsVip) : IRequest;

public class SetCustomerVipCommandHandler : IRequestHandler<SetCustomerVipCommand>
{
    private readonly ICustomerRepository _customers;

    public SetCustomerVipCommandHandler(ICustomerRepository customers) => _customers = customers;

    public async Task Handle(SetCustomerVipCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.SetVip(cmd.IsVip);
        await _customers.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SetCustomerVipCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Customers/Commands/SetCustomerVipCommand.cs \
        tests/CRM.Application.Tests/Customers/SetCustomerVipCommandHandlerTests.cs
git commit -m "feat(customers): add SetCustomerVipCommand"
```

---

## Task 2: CustomersController — PATCH /api/customers/{id}/vip

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerVipTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerVipTests.cs
using System.Net;
using System.Net.Http.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerVipTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Manager")
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
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task SetVip_ValidRequest_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync($"/api/customers/{Guid.NewGuid()}/vip",
            new { isVip = true });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetVip_AsAgent_Returns403()
    {
        var client = BuildClient(role: "Agent");

        var response = await client.PatchAsJsonAsync($"/api/customers/{Guid.NewGuid()}/vip",
            new { isVip = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetVip_NonExistentCustomer_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));
        var client = BuildClient();

        var response = await client.PatchAsJsonAsync($"/api/customers/{Guid.NewGuid()}/vip",
            new { isVip = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerVipTests" -v n
```

Expected: FAIL — `PATCH /api/customers/{id}/vip` does not exist yet.

- [ ] **Step 3: Add VIP endpoint to CustomersController**

```csharp
// Add to src/CRM.API/Controllers/CustomersController.cs inside the class:

public record SetVipRequest(bool IsVip);

[Authorize(Roles = "Admin,Manager")]
[HttpPatch("{id:guid}/vip")]
public async Task<IActionResult> SetVip(
    Guid id, [FromBody] SetVipRequest request, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new SetCustomerVipCommand(id, request.IsVip), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { error = ex.Message });
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerVipTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerVipTests.cs
git commit -m "feat(api): add PATCH /api/customers/{id}/vip endpoint"
```
