# Get Ticket SLA — Implementation Plan

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

**Story:** US-BE-033  
**Goal:** Implement `GET /api/tickets/{id}/sla` — returns the SLA status for a ticket including first-response due, resolution due, breach tier, and elapsed/remaining business minutes.

**Architecture:** `GetTicketSlaQuery(ticketId)` → handler fetches `TicketSla` record (created when SLA clock starts), computes current breach tier based on elapsed time vs policy targets. Returns `TicketSlaDto`. Returns 404 if no SLA record exists (ticket predates SLA configuration).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Sla/TicketSla.cs` |
| Create | `src/CRM.Domain/Sla/ITicketSlaRepository.cs` |
| Create | `src/CRM.Application/Tickets/Queries/GetTicketSlaQuery.cs` |
| Create | `src/CRM.Application/Tickets/DTOs/TicketSlaDto.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/GetTicketSlaQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerSlaTests.cs` |

---

## Task 1: TicketSla Domain Entity

**Files:**
- Create: `src/CRM.Domain/Sla/TicketSla.cs`
- Create: `src/CRM.Domain/Sla/ITicketSlaRepository.cs`

- [ ] **Step 1: Create TicketSla entity and repository**

```csharp
// src/CRM.Domain/Sla/TicketSla.cs
namespace CRM.Domain.Sla;

public enum SlaBreachTier { None, Warning, Breach, CriticalBreach }

public class TicketSla
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid SlaPolicyId { get; private set; }
    public DateTime ClockStartedAt { get; private set; }
    public DateTime? ClockPausedAt { get; private set; }
    public int AccumulatedPauseMinutes { get; private set; }
    public DateTime? FirstResponseDue { get; private set; }
    public DateTime? ResolutionDue { get; private set; }
    public DateTime? FirstResponseAt { get; private set; }
    public bool FirstResponseBreached { get; private set; }
    public bool ResolutionBreached { get; private set; }
    public SlaBreachTier BreachTier { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TicketSla() { }

    public static TicketSla Create(
        Guid ticketId, Guid slaPolicyId,
        DateTime clockStartedAt,
        DateTime? firstResponseDue,
        DateTime? resolutionDue)
        => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            SlaPolicyId = slaPolicyId,
            ClockStartedAt = clockStartedAt,
            FirstResponseDue = firstResponseDue,
            ResolutionDue = resolutionDue,
            BreachTier = SlaBreachTier.None,
            UpdatedAt = DateTime.UtcNow
        };

    public void PauseClock()
    {
        ClockPausedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResumeClock()
    {
        if (ClockPausedAt.HasValue)
        {
            AccumulatedPauseMinutes += (int)(DateTime.UtcNow - ClockPausedAt.Value).TotalMinutes;
            ClockPausedAt = null;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateBreachTier(SlaBreachTier tier)
    {
        BreachTier = tier;
        if (tier >= SlaBreachTier.Breach) ResolutionBreached = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFirstResponse()
    {
        FirstResponseAt = DateTime.UtcNow;
        if (FirstResponseDue.HasValue && FirstResponseAt > FirstResponseDue)
            FirstResponseBreached = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

```csharp
// src/CRM.Domain/Sla/ITicketSlaRepository.cs
namespace CRM.Domain.Sla;

public interface ITicketSlaRepository
{
    Task<TicketSla?> FindByTicketIdAsync(Guid ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<TicketSla>> ListActiveAsync(CancellationToken ct = default);
    Task AddAsync(TicketSla sla, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Sla/
git commit -m "feat(domain): add TicketSla entity with breach tier tracking and ITicketSlaRepository"
```

---

## Task 2: GetTicketSla Query + Handler + DTO

**Files:**
- Create: `src/CRM.Application/Tickets/DTOs/TicketSlaDto.cs`
- Create: `src/CRM.Application/Tickets/Queries/GetTicketSlaQuery.cs`
- Test: `tests/CRM.Application.Tests/Tickets/GetTicketSlaQueryHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/GetTicketSlaQueryHandlerTests.cs
using CRM.Application.Tickets.Queries;
using CRM.Domain.Sla;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class GetTicketSlaQueryHandlerTests
{
    private readonly Mock<ITicketSlaRepository> _repo = new();
    private readonly GetTicketSlaQueryHandler _handler;

    public GetTicketSlaQueryHandlerTests()
    {
        _handler = new GetTicketSlaQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingSla_ReturnsSlaDto()
    {
        var ticketId = Guid.NewGuid();
        var sla = TicketSla.Create(
            ticketId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-2),
            DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(8));

        _repo.Setup(r => r.FindByTicketIdAsync(ticketId, default)).ReturnsAsync(sla);

        var result = await _handler.Handle(new GetTicketSlaQuery(ticketId), default);

        Assert.Equal(ticketId, result.TicketId);
        Assert.Equal("None", result.BreachTier);
        Assert.NotNull(result.ResolutionDue);
    }

    [Fact]
    public async Task Handle_NoSlaRecord_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByTicketIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((TicketSla?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new GetTicketSlaQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_BreachedSla_ReturnsBreachTierBreach()
    {
        var ticketId = Guid.NewGuid();
        var sla = TicketSla.Create(
            ticketId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-10),
            DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1));
        sla.UpdateBreachTier(SlaBreachTier.Breach);

        _repo.Setup(r => r.FindByTicketIdAsync(ticketId, default)).ReturnsAsync(sla);

        var result = await _handler.Handle(new GetTicketSlaQuery(ticketId), default);

        Assert.Equal("Breach", result.BreachTier);
        Assert.True(result.ResolutionBreached);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketSlaQueryHandlerTests" -v n
```

Expected: FAIL — `GetTicketSlaQuery` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Tickets/DTOs/TicketSlaDto.cs
namespace CRM.Application.Tickets.DTOs;

public record TicketSlaDto(
    Guid TicketId,
    DateTime ClockStartedAt,
    DateTime? FirstResponseDue,
    DateTime? ResolutionDue,
    DateTime? FirstResponseAt,
    bool FirstResponseBreached,
    bool ResolutionBreached,
    string BreachTier,
    int AccumulatedPauseMinutes,
    bool IsPaused);
```

- [ ] **Step 4: Implement query and handler**

```csharp
// src/CRM.Application/Tickets/Queries/GetTicketSlaQuery.cs
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketSlaQuery(Guid TicketId) : IRequest<TicketSlaDto>;

public class GetTicketSlaQueryHandler : IRequestHandler<GetTicketSlaQuery, TicketSlaDto>
{
    private readonly ITicketSlaRepository _sla;

    public GetTicketSlaQueryHandler(ITicketSlaRepository sla) => _sla = sla;

    public async Task<TicketSlaDto> Handle(GetTicketSlaQuery query, CancellationToken ct)
    {
        var sla = await _sla.FindByTicketIdAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"No SLA record found for ticket {query.TicketId}.");

        return new TicketSlaDto(
            TicketId: sla.TicketId,
            ClockStartedAt: sla.ClockStartedAt,
            FirstResponseDue: sla.FirstResponseDue,
            ResolutionDue: sla.ResolutionDue,
            FirstResponseAt: sla.FirstResponseAt,
            FirstResponseBreached: sla.FirstResponseBreached,
            ResolutionBreached: sla.ResolutionBreached,
            BreachTier: sla.BreachTier.ToString(),
            AccumulatedPauseMinutes: sla.AccumulatedPauseMinutes,
            IsPaused: sla.ClockPausedAt.HasValue);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetTicketSlaQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Tickets/Queries/GetTicketSlaQuery.cs \
        src/CRM.Application/Tickets/DTOs/TicketSlaDto.cs \
        tests/CRM.Application.Tests/Tickets/GetTicketSlaQueryHandlerTests.cs
git commit -m "feat(tickets): add GetTicketSlaQuery"
```

---

## Task 3: TicketsController — GET /api/tickets/{id}/sla

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerSlaTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerSlaTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.DTOs;
using CRM.Application.Tickets.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerSlaTests
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
    public async Task GetSla_Returns200WithDto()
    {
        var ticketId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.Is<GetTicketSlaQuery>(q => q.TicketId == ticketId), default))
                 .ReturnsAsync(new TicketSlaDto(
                     ticketId, DateTime.UtcNow.AddHours(-2),
                     DateTime.UtcNow.AddHours(2), DateTime.UtcNow.AddHours(8),
                     null, false, false, "None", 0, false));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/tickets/{ticketId}/sla");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketSlaDto>();
        Assert.Equal("None", body!.BreachTier);
    }

    [Fact]
    public async Task GetSla_NoSlaRecord_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetTicketSlaQuery>(), default))
                 .ThrowsAsync(new KeyNotFoundException("No SLA record."));

        var client = BuildClient();
        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}/sla");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerSlaTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add GetSla endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

[HttpGet("{id:guid}/sla")]
public async Task<IActionResult> GetSla(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new GetTicketSlaQuery(id), ct);
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
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerSlaTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerSlaTests.cs
git commit -m "feat(api): add GET /api/tickets/{id}/sla endpoint"
```
