# Customer Tickets List (Internal) — Implementation Plan

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

**Story:** US-BE-096  
**Goal:** Implement `GET /api/customers/{id}/tickets` — returns paginated ticket history for a specific customer. Agent scope: only tickets in departments the agent belongs to. Admin/Manager: all tickets. Filter: `?status=`, `?page=`, `?pageSize=` (default 20). Each item: ticketNumber, subject, status, priority, createdAt, category. Returns 404 if customer not found or soft-deleted.

**Architecture:** `GetCustomerTicketsQuery(CustomerId, RequestingUserId, RequestingUserRole, Status?, Page, PageSize)` → scope-resolves department IDs → delegates to `ITicketRepository.ListByCustomerAsync()`. Adds action to existing `CustomersController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Customers/Queries/GetCustomerTicketsQuery.cs` |
| Create | `src/CRM.Application/Customers/DTOs/CustomerTicketDto.cs` |
| Modify | `src/CRM.API/Controllers/CustomersController.cs` |
| Test   | `tests/CRM.Application.Tests/Customers/GetCustomerTicketsQueryHandlerTests.cs` |

---

## Task 1: Customer Tickets List Query

> Note: `ICustomerRepository`, `Customer` are from US-BE-009. `ITicketRepository`, `Ticket` are from US-BE-019. `IUserRepository.GetDepartmentIdsAsync` is from US-BE-073. `CustomersController` is from US-BE-010. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Customers/GetCustomerTicketsQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Customers.Queries;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Customers;

public class GetCustomerTicketsQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GetCustomerTicketsQueryHandler _handler;

    public GetCustomerTicketsQueryHandlerTests()
    {
        _handler = new GetCustomerTicketsQueryHandler(
            _customers.Object, _tickets.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AdminNoFilter_ReturnsAllCustomerTickets()
    {
        var customerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", null, null);

        _customers.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _tickets.Setup(r => r.ListByCustomerAsync(
            customerId, null, null, 1, 20, default))
            .ReturnsAsync(new PagedResult<CustomerTicketProjection>(
                new List<CustomerTicketProjection>
                {
                    new("TKT-001", "Login issue", "Open", "High", DateTime.UtcNow, "Technical")
                }, 1, 1, 20));

        var result = await _handler.Handle(
            new GetCustomerTicketsQuery(customerId, adminId, UserRole.Admin, null, 1, 20),
            default);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TKT-001", result.Items[0].TicketNumber);
    }

    [Fact]
    public async Task Handle_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _customers.Setup(r => r.FindByIdAsync(customerId, default))
                  .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new GetCustomerTicketsQuery(customerId, Guid.NewGuid(), UserRole.Admin, null, 1, 20),
                default));
    }

    [Fact]
    public async Task Handle_AgentScope_ScopesToOwnDepartments()
    {
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();

        _customers.Setup(r => r.FindByIdAsync(customerId, default))
                  .ReturnsAsync(Customer.Create("Bob", "bob@example.com", null, null));
        _users.Setup(u => u.GetDepartmentIdsAsync(agentId, default))
              .ReturnsAsync(new List<Guid> { deptId });
        _tickets.Setup(r => r.ListByCustomerAsync(
            customerId, null,
            It.Is<IReadOnlyList<Guid>?>(ids => ids != null && ids.Contains(deptId)),
            1, 20, default))
            .ReturnsAsync(new PagedResult<CustomerTicketProjection>(
                new List<CustomerTicketProjection>(), 0, 1, 20));

        await _handler.Handle(
            new GetCustomerTicketsQuery(customerId, agentId, UserRole.Agent, null, 1, 20),
            default);

        _users.Verify(u => u.GetDepartmentIdsAsync(agentId, default), Times.Once);
    }

    [Fact]
    public async Task Handle_StatusFilter_PassedToRepository()
    {
        var customerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _customers.Setup(r => r.FindByIdAsync(customerId, default))
                  .ReturnsAsync(Customer.Create("Carol", "carol@example.com", null, null));
        _tickets.Setup(r => r.ListByCustomerAsync(
            customerId, "Open", null, 1, 20, default))
            .ReturnsAsync(new PagedResult<CustomerTicketProjection>(
                new List<CustomerTicketProjection>(), 0, 1, 20));

        await _handler.Handle(
            new GetCustomerTicketsQuery(customerId, adminId, UserRole.Admin, "Open", 1, 20),
            default);

        _tickets.Verify(r => r.ListByCustomerAsync(
            customerId, "Open", null, 1, 20, default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetCustomerTicketsQueryHandlerTests" -v n
```

Expected: FAIL — `GetCustomerTicketsQuery` does not exist yet.

- [ ] **Step 3: Add ListByCustomerAsync to ITicketRepository**

Open `src/CRM.Domain/Tickets/ITicketRepository.cs` and add:

```csharp
public record CustomerTicketProjection(
    string TicketNumber, string Subject, string Status,
    string Priority, DateTime CreatedAt, string? Category);

Task<PagedResult<CustomerTicketProjection>> ListByCustomerAsync(
    Guid customerId,
    string? status,
    IReadOnlyList<Guid>? departmentIds,
    int page, int pageSize,
    CancellationToken ct = default);
```

- [ ] **Step 4: Create CustomerTicketDto**

```csharp
// src/CRM.Application/Customers/DTOs/CustomerTicketDto.cs
namespace CRM.Application.Customers.DTOs;

public record CustomerTicketDto(
    string TicketNumber,
    string Subject,
    string Status,
    string Priority,
    DateTime CreatedAt,
    string? Category);
```

- [ ] **Step 5: Implement GetCustomerTicketsQuery**

```csharp
// src/CRM.Application/Customers/Queries/GetCustomerTicketsQuery.cs
using CRM.Application.Common;
using CRM.Application.Customers.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Customers.Queries;

public record GetCustomerTicketsQuery(
    Guid CustomerId,
    Guid RequestingUserId,
    UserRole RequestingUserRole,
    string? Status,
    int Page,
    int PageSize) : IRequest<PagedResult<CustomerTicketDto>>;

public class GetCustomerTicketsQueryHandler
    : IRequestHandler<GetCustomerTicketsQuery, PagedResult<CustomerTicketDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public GetCustomerTicketsQueryHandler(
        ICustomerRepository customers,
        ITicketRepository tickets,
        IUserRepository users)
    {
        _customers = customers;
        _tickets = tickets;
        _users = users;
    }

    public async Task<PagedResult<CustomerTicketDto>> Handle(
        GetCustomerTicketsQuery query, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(query.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {query.CustomerId} not found.");

        IReadOnlyList<Guid>? effectiveDepartmentIds = null;

        if (query.RequestingUserRole == UserRole.Agent)
        {
            effectiveDepartmentIds = await _users.GetDepartmentIdsAsync(
                query.RequestingUserId, ct);
        }

        var paged = await _tickets.ListByCustomerAsync(
            query.CustomerId, query.Status, effectiveDepartmentIds,
            query.Page, query.PageSize, ct);

        var dtos = paged.Items.Select(t => new CustomerTicketDto(
            t.TicketNumber, t.Subject, t.Status,
            t.Priority, t.CreatedAt, t.Category)).ToList();

        return new PagedResult<CustomerTicketDto>(
            dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetCustomerTicketsQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 7: Add CustomerTickets action to CustomersController**

Open `src/CRM.API/Controllers/CustomersController.cs` and add:

```csharp
[HttpGet("{id:guid}/tickets")]
public async Task<IActionResult> GetTickets(
    Guid id,
    [FromQuery] string? status,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    try
    {
        var result = await _mediator.Send(
            new GetCustomerTicketsQuery(
                id, CurrentUserId, CurrentUserRole, status, page, pageSize), ct);
        return Ok(new { data = result });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
}
```

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Application/Customers/Queries/GetCustomerTicketsQuery.cs \
        src/CRM.Application/Customers/DTOs/CustomerTicketDto.cs \
        src/CRM.API/Controllers/CustomersController.cs \
        tests/CRM.Application.Tests/Customers/GetCustomerTicketsQueryHandlerTests.cs
git commit -m "feat(customers): add GET /api/customers/{id}/tickets — role-scoped ticket history with status filter"
```
