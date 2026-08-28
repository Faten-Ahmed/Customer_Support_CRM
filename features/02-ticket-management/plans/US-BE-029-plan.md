# Get Ticket Messages — Implementation Plan

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

**Story:** US-BE-029  
**Goal:** Implement `GET /api/tickets/{id}/messages` — returns paginated messages for a ticket. Internal notes are only visible to Admin/Manager/Agent; customers see non-internal messages only.

**Architecture:** `GetTicketMessagesQuery(ticketId, page, pageSize, requestingUserIsCustomer)` → handler fetches paginated messages from `ITicketMessageRepository`, filters out internal notes if caller is a customer. Returns `PagedResult<TicketMessageDto>`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Queries/GetTicketMessagesQuery.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/GetTicketMessagesQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerGetMessagesTests.cs` |

---

## Task 1: GetTicketMessages Query + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Queries/GetTicketMessagesQuery.cs`
- Test: `tests/CRM.Application.Tests/Tickets/GetTicketMessagesQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/GetTicketMessagesQueryHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketMessagesQueryHandlerTests
{
    private readonly Mock<ITicketMessageRepository> _repo = new();
    private readonly GetTicketMessagesQueryHandler _handler;

    public GetTicketMessagesQueryHandlerTests()
    {
        _handler = new GetTicketMessagesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_AgentCaller_ReturnsAllIncludingInternal()
    {
        var ticketId = Guid.NewGuid();
        var messages = new List<TicketMessage>
        {
            TicketMessage.Create(ticketId, "Public reply", false, Guid.NewGuid(), MessageAuthorType.Agent),
            TicketMessage.Create(ticketId, "Internal note", true, Guid.NewGuid(), MessageAuthorType.Agent)
        };

        _repo.Setup(r => r.ListByTicketAsync(ticketId, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketMessage>(messages, 2, 1, 20));

        var result = await _handler.Handle(
            new GetTicketMessagesQuery(ticketId, 1, 20, isCallerCustomer: false), default);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task Handle_CustomerCaller_FiltersOutInternalNotes()
    {
        var ticketId = Guid.NewGuid();
        var messages = new List<TicketMessage>
        {
            TicketMessage.Create(ticketId, "Public reply", false, Guid.NewGuid(), MessageAuthorType.Agent),
            TicketMessage.Create(ticketId, "Internal note", true, Guid.NewGuid(), MessageAuthorType.Agent)
        };

        _repo.Setup(r => r.ListByTicketAsync(ticketId, 1, 20, default))
             .ReturnsAsync(new PagedResult<TicketMessage>(messages, 2, 1, 20));

        var result = await _handler.Handle(
            new GetTicketMessagesQuery(ticketId, 1, 20, isCallerCustomer: true), default);

        Assert.Single(result.Items);
        Assert.False(result.Items[0].IsInternal);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketMessagesQueryHandlerTests" -v n
```

Expected: FAIL — `GetTicketMessagesQuery` does not exist yet.

- [ ] **Step 3: Implement query and handler**

```csharp
// src/CRM.Application/Tickets/Queries/GetTicketMessagesQuery.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketMessagesQuery(
    Guid TicketId,
    int Page,
    int PageSize,
    bool IsCallerCustomer) : IRequest<PagedResult<TicketMessageDto>>;

public class GetTicketMessagesQueryHandler
    : IRequestHandler<GetTicketMessagesQuery, PagedResult<TicketMessageDto>>
{
    private readonly ITicketMessageRepository _messages;

    public GetTicketMessagesQueryHandler(ITicketMessageRepository messages)
        => _messages = messages;

    public async Task<PagedResult<TicketMessageDto>> Handle(
        GetTicketMessagesQuery query, CancellationToken ct)
    {
        var paged = await _messages.ListByTicketAsync(query.TicketId, query.Page, query.PageSize, ct);

        var items = paged.Items
            .Where(m => !query.IsCallerCustomer || !m.IsInternal)
            .Select(m => new TicketMessageDto(
                m.Id, m.TicketId, m.Body, m.IsInternal,
                m.AuthorId, string.Empty, m.AuthorType.ToString(), m.CreatedAt))
            .ToList();

        return new PagedResult<TicketMessageDto>(items, items.Count, query.Page, query.PageSize);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketMessagesQueryHandlerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Queries/GetTicketMessagesQuery.cs \
        tests/CRM.Application.Tests/Tickets/GetTicketMessagesQueryHandlerTests.cs
git commit -m "feat(tickets): add GetTicketMessagesQuery with customer/agent visibility filter"
```

---

## Task 2: TicketsController — GET /api/tickets/{id}/messages

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerGetMessagesTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerGetMessagesTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerGetMessagesTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
    public async Task GetMessages_Returns200WithPagedResult()
    {
        var ticketId = Guid.NewGuid();
        var items = new List<TicketMessageDto>
        {
            new(Guid.NewGuid(), ticketId, "<p>Hi</p>", false,
                Guid.NewGuid(), "Ali Hassan", "Agent", DateTime.UtcNow)
        };
        _mediator.Setup(m => m.Send(It.IsAny<GetTicketMessagesQuery>(), default))
                 .ReturnsAsync(new PagedResult<TicketMessageDto>(items, 1, 1, 20));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/tickets/{ticketId}/messages?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TicketMessageDto>>();
        Assert.Equal(1, body!.TotalCount);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerGetMessagesTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add GetMessages endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

[HttpGet("{id:guid}/messages")]
public async Task<IActionResult> GetMessages(
    Guid id,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    var result = await _mediator.Send(
        new GetTicketMessagesQuery(id, page, pageSize, isCallerCustomer: false), ct);
    return Ok(result);
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerGetMessagesTests" -v n
```

Expected: 1 test PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerGetMessagesTests.cs
git commit -m "feat(api): add GET /api/tickets/{id}/messages endpoint"
```
