# Get Customer — Implementation Plan

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

**Story:** US-BE-012  
**Goal:** Implement `GET /api/customers/{id}` — returns a single customer's full profile (contacts, VIP status) to Admin/Manager/Agent.

**Architecture:** `GetCustomerQuery(id)` → handler fetches `Customer` with contacts from `ICustomerRepository`, maps to `CustomerDetailDto`. Returns 404 if not found or soft-deleted. Endpoint protected by `[Authorize(Roles = "Admin,Manager,Agent")]`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Queries/GetCustomerQuery.cs` |
| Create | `src/CRM.Application/Customers/DTOs/CustomerDetailDto.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/GetCustomerQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerGetTests.cs` |

---

## Task 1: GetCustomer Query + Handler + DTO

**Files:**
- Create: `src/CRM.Application/Customers/DTOs/CustomerDetailDto.cs`
- Create: `src/CRM.Application/Customers/Queries/GetCustomerQuery.cs`
- Test: `tests/CRM.Application.Tests/Customers/GetCustomerQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/GetCustomerQueryHandlerTests.cs
using CRM.Application.Customers.Queries;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class GetCustomerQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly GetCustomerQueryHandler _handler;

    public GetCustomerQueryHandlerTests()
    {
        _handler = new GetCustomerQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingCustomer_ReturnsCustomerDetailDto()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test", "+971501234567", true);
        customer.AddContact("WhatsApp", "+971501234567", true);

        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(new GetCustomerQuery(id), default);

        Assert.Equal("ali@crm.test", result.Email);
        Assert.True(result.IsVip);
        Assert.Single(result.Contacts);
        Assert.Equal("WhatsApp", result.Contacts[0].Type);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdWithContactsAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCustomerQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_DeletedCustomer_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.SoftDelete();

        _repo.Setup(r => r.FindByIdWithContactsAsync(id, default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetCustomerQuery(id), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetCustomerQueryHandlerTests" -v n
```

Expected: FAIL — `GetCustomerQuery` does not exist yet.

- [ ] **Step 3: Create DTOs**

```csharp
// src/CRM.Application/Customers/DTOs/CustomerDetailDto.cs
namespace CRM.Application.Customers.DTOs;

public record ContactDto(Guid Id, string Type, string Value, bool IsPrimary);

public record CustomerDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    bool IsVip,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ContactDto> Contacts);
```

- [ ] **Step 4: Create query and handler**

```csharp
// src/CRM.Application/Customers/Queries/GetCustomerQuery.cs
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Queries;

public record GetCustomerQuery(Guid CustomerId) : IRequest<CustomerDetailDto>;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDetailDto>
{
    private readonly ICustomerRepository _customers;

    public GetCustomerQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<CustomerDetailDto> Handle(GetCustomerQuery query, CancellationToken ct)
    {
        var customer = await _customers.FindByIdWithContactsAsync(query.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {query.CustomerId} not found.");

        return new CustomerDetailDto(
            Id: customer.Id,
            FirstName: customer.FirstName,
            LastName: customer.LastName,
            Email: customer.Email,
            Phone: customer.Phone,
            IsVip: customer.IsVip,
            IsDeleted: customer.IsDeleted,
            CreatedAt: customer.CreatedAt,
            UpdatedAt: customer.UpdatedAt,
            Contacts: customer.Contacts
                .Select(c => new ContactDto(c.Id, c.Type, c.Value, c.IsPrimary))
                .ToList());
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetCustomerQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Customers/Queries/ \
        src/CRM.Application/Customers/DTOs/CustomerDetailDto.cs \
        tests/CRM.Application.Tests/Customers/GetCustomerQueryHandlerTests.cs
git commit -m "feat(customers): add GetCustomerQuery with contact details"
```

---

## Task 2: CustomersController — GET /api/customers/{id}

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerGetTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerGetTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Customers.DTOs;
using CRM.Application.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerGetTests
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
    public async Task GetById_ExistingCustomer_Returns200()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetCustomerQuery>(q => q.CustomerId == id), default))
                 .ReturnsAsync(new CustomerDetailDto(
                     id, "Ali", "Hassan", "ali@crm.test", null, false, false,
                     DateTime.UtcNow, DateTime.UtcNow, new List<ContactDto>()));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/customers/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerDetailDto>();
        Assert.Equal("ali@crm.test", body!.Email);
    }

    [Fact]
    public async Task GetById_NonExistentCustomer_Returns404()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetCustomerQuery>(q => q.CustomerId == id), default))
                 .ThrowsAsync(new KeyNotFoundException("Not found."));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/customers/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Unauthenticated_Returns401()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerGetTests" -v n
```

Expected: FAIL — `GetById` action is a stub returning `Ok()`.

- [ ] **Step 3: Implement GetById in CustomersController**

```csharp
// Replace stub GetById in src/CRM.API/Controllers/CustomersController.cs:

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new GetCustomerQuery(id), ct);
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
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerGetTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerGetTests.cs
git commit -m "feat(api): implement GET /api/customers/{id} endpoint"
```
