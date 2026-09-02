# List Customers — Implementation Plan

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

**Story:** US-BE-013  
**Goal:** Implement `GET /api/customers` — returns a paginated, filterable list of customers for Admin/Manager/Agent with search by name/email, VIP filter, and sort options.

**Architecture:** `ListCustomersQuery(search, isVip, page, pageSize, sortBy, sortDesc)` → handler delegates to `ICustomerRepository.ListAsync(filter)` which builds the EF Core query; returns `PagedResult<CustomerDto>`. No soft-deleted records are included.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Common/PagedResult.cs` |
| Create | `src/CRM.Application/Customers/Queries/ListCustomersQuery.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/ListCustomersQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Customers/CustomersControllerListTests.cs` |

---

## Task 1: PagedResult + ListCustomers Query + Handler

**Files:**
- Create: `src/CRM.Application/Common/PagedResult.cs`
- Create: `src/CRM.Application/Customers/Queries/ListCustomersQuery.cs`
- Test: `tests/CRM.Application.Tests/Customers/ListCustomersQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/ListCustomersQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Customers.DTOs;
using CRM.Application.Customers.Queries;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class ListCustomersQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly ListCustomersQueryHandler _handler;

    public ListCustomersQueryHandlerTests()
    {
        _handler = new ListCustomersQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_NoFilter_ReturnsPagedResult()
    {
        var customers = new List<Customer>
        {
            Customer.Create("Ali Hassan", "ali@crm.test"),
            Customer.Create("Sara Al-Ali", "sara@crm.test")
        };

        _repo.Setup(r => r.ListAsync(It.IsAny<CustomerFilter>(), default))
             .ReturnsAsync(new PagedResult<Customer>(customers, 2, 1, 20));

        var result = await _handler.Handle(
            new ListCustomersQuery(null, null, 1, 20, "createdAt", false), default);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_PassesSearchToRepository()
    {
        _repo.Setup(r => r.ListAsync(It.Is<CustomerFilter>(f => f.Search == "Ali"), default))
             .ReturnsAsync(new PagedResult<Customer>(new List<Customer>(), 0, 1, 20));

        await _handler.Handle(
            new ListCustomersQuery("Ali", null, 1, 20, "createdAt", false), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<CustomerFilter>(f => f.Search == "Ali"), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithVipFilter_PassesVipToRepository()
    {
        _repo.Setup(r => r.ListAsync(It.Is<CustomerFilter>(f => f.IsVip == true), default))
             .ReturnsAsync(new PagedResult<Customer>(new List<Customer>(), 0, 1, 20));

        await _handler.Handle(
            new ListCustomersQuery(null, true, 1, 20, "createdAt", false), default);

        _repo.Verify(r => r.ListAsync(
            It.Is<CustomerFilter>(f => f.IsVip == true), default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListCustomersQueryHandlerTests" -v n
```

Expected: FAIL — `ListCustomersQuery` does not exist yet.

- [ ] **Step 3: Create PagedResult**

```csharp
// src/CRM.Application/Common/PagedResult.cs
namespace CRM.Application.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

- [ ] **Step 4: Create CustomerFilter and ListCustomersQuery**

```csharp
// src/CRM.Application/Customers/Queries/ListCustomersQuery.cs
using CRM.Application.Common;
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Customers.Queries;

public record CustomerFilter(
    string? Search,
    bool? IsVip,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDesc);

public record ListCustomersQuery(
    string? Search,
    bool? IsVip,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDesc) : IRequest<PagedResult<CustomerDto>>;

public class ListCustomersQueryHandler
    : IRequestHandler<ListCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _customers;

    public ListCustomersQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<PagedResult<CustomerDto>> Handle(
        ListCustomersQuery query, CancellationToken ct)
    {
        var filter = new CustomerFilter(
            query.Search, query.IsVip,
            query.Page, query.PageSize,
            query.SortBy, query.SortDesc);

        var paged = await _customers.ListAsync(filter, ct);

        var dtos = paged.Items
            .Select(c => new CustomerDto(
                c.Id, c.FullName, c.FullNameAr, c.Email, c.Phone,
                c.CompanyName, c.CompanyNameAr, c.IsVip, c.IsActive, c.CreatedAt))
            .ToList();

        return new PagedResult<CustomerDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ListCustomersQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Common/PagedResult.cs \
        src/CRM.Application/Customers/Queries/ListCustomersQuery.cs \
        tests/CRM.Application.Tests/Customers/ListCustomersQueryHandlerTests.cs
git commit -m "feat(customers): add ListCustomersQuery with pagination and filtering"
```

---

## Task 2: CustomersController — GET /api/customers

**Files:**
- Modify: `src/CRM.API/Controllers/CustomersController.cs`
- Test: `tests/CRM.API.Tests/Customers/CustomersControllerListTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Customers/CustomersControllerListTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Customers.DTOs;
using CRM.Application.Customers.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Customers;

public class CustomersControllerListTests
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
    public async Task List_NoFilter_Returns200WithPagedResult()
    {
        var items = new List<CustomerDto>
        {
            new(Guid.NewGuid(), "Ali Hassan", null, "ali@crm.test", null, null, null, false, true, DateTime.UtcNow)
        };
        _mediator.Setup(m => m.Send(It.IsAny<ListCustomersQuery>(), default))
                 .ReturnsAsync(new PagedResult<CustomerDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync("/api/customers?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerDto>>();
        Assert.Equal(1, body!.TotalCount);
    }

    [Fact]
    public async Task List_Unauthenticated_Returns401()
    {
        var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerListTests" -v n
```

Expected: FAIL — `GET /api/customers` endpoint does not exist yet.

- [ ] **Step 3: Add List endpoint to CustomersController**

```csharp
// Add to src/CRM.API/Controllers/CustomersController.cs inside the class:

[HttpGet]
public async Task<IActionResult> List(
    [FromQuery] string? search,
    [FromQuery] bool? isVip,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string sortBy = "createdAt",
    [FromQuery] bool sortDesc = false,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new ListCustomersQuery(search, isVip, page, pageSize, sortBy, sortDesc), ct);
    return Ok(result);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "CustomersControllerListTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.API.Tests/Customers/CustomersControllerListTests.cs
git commit -m "feat(api): add GET /api/customers with pagination and filtering"
```
