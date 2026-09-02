# SLA Policy CRUD — Implementation Plan

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

**Story:** US-BE-043  
**Goal:** Implement CRUD endpoints for `SlaPolicy` under `/api/admin/sla/policies` — list all policies, create department-specific or global policies, and update targets. Admin-only write access; Admin+Manager can list.

**Architecture:** `ListSlaPoliciesQuery` → `ISlaPolicyRepository.ListAllAsync`. `CreateSlaPolicyCommand(priority, departmentId?, firstResponseMinutes, resolutionMinutes, thresholds)` → validates and persists. `UpdateSlaPolicyCommand(id, minutes, thresholds)` → validates `firstResponse < resolution` and `warning < breach < critical`. Changes do NOT retroactively affect existing `TicketSla` records (snapshot isolation).

**Note:** `SlaPolicy` entity and `ISlaPolicyRepository` are defined in US-BE-039-plan and should already exist.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Sla/Queries/ListSlaPoliciesQuery.cs` |
| Create | `src/CRM.Application/Sla/Commands/CreateSlaPolicyCommand.cs` |
| Create | `src/CRM.Application/Sla/Commands/UpdateSlaPolicyCommand.cs` |
| Create | `src/CRM.Application/Sla/DTOs/SlaPolicyDto.cs` |
| Create | `src/CRM.API/Controllers/Admin/SlaPoliciesController.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/SlaPolicyCrudTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/SlaPoliciesControllerTests.cs` |

---

## Task 1: SlaPolicyCrud Application Layer

**Files:**
- Create: `src/CRM.Application/Sla/DTOs/SlaPolicyDto.cs`
- Create: `src/CRM.Application/Sla/Queries/ListSlaPoliciesQuery.cs`
- Create: `src/CRM.Application/Sla/Commands/CreateSlaPolicyCommand.cs`
- Create: `src/CRM.Application/Sla/Commands/UpdateSlaPolicyCommand.cs`
- Test: `tests/CRM.Application.Tests/Sla/SlaPolicyCrudTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/SlaPolicyCrudTests.cs
using CRM.Application.Sla.Commands;
using CRM.Application.Sla.Queries;
using CRM.Domain.Sla;
using CRM.Domain.Tickets.Enums;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class SlaPolicyCrudTests
{
    private readonly Mock<ISlaPolicyRepository> _repo = new();

    [Fact]
    public async Task ListPolicies_ReturnsAllPolicies()
    {
        _repo.Setup(r => r.ListAllAsync(default))
             .ReturnsAsync(new List<SlaPolicy>
             {
                 SlaPolicy.Create(TicketPriority.High, 60, 480),
                 SlaPolicy.Create(TicketPriority.Low, 240, 1440)
             });

        var handler = new ListSlaPoliciesQueryHandler(_repo.Object);
        var result = await handler.Handle(new ListSlaPoliciesQuery(), default);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreatePolicy_ValidData_Persists()
    {
        var handler = new CreateSlaPolicyCommandHandler(_repo.Object);
        var cmd = new CreateSlaPolicyCommand(
            TicketPriority.Critical, null, 30, 240, 80, 100, 200);

        var result = await handler.Handle(cmd, default);

        Assert.NotEqual(Guid.Empty, result);
        _repo.Verify(r => r.AddAsync(It.IsAny<SlaPolicy>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreatePolicy_FirstResponseGreaterThanResolution_ThrowsValidationException()
    {
        var handler = new CreateSlaPolicyCommandHandler(_repo.Object);
        var cmd = new CreateSlaPolicyCommand(
            TicketPriority.High, null, 480, 60, 80, 100, 200); // first > resolution

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(cmd, default));
    }

    [Fact]
    public async Task CreatePolicy_ThresholdsOutOfOrder_ThrowsValidationException()
    {
        var handler = new CreateSlaPolicyCommandHandler(_repo.Object);
        var cmd = new CreateSlaPolicyCommand(
            TicketPriority.High, null, 60, 480, 100, 80, 200); // warning > breach

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(cmd, default));
    }

    [Fact]
    public async Task UpdatePolicy_ValidData_UpdatesMinutesAndThresholds()
    {
        var existing = SlaPolicy.Create(TicketPriority.High, 60, 480);
        _repo.Setup(r => r.FindByIdAsync(existing.Id, default)).ReturnsAsync(existing);

        var handler = new UpdateSlaPolicyCommandHandler(_repo.Object);
        await handler.Handle(new UpdateSlaPolicyCommand(
            existing.Id, 90, 600, 75, 100, 150), default);

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Equal(90, existing.FirstResponseMinutes);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaPolicyCrudTests" -v n
```

Expected: FAIL — `ListSlaPoliciesQuery` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Sla/DTOs/SlaPolicyDto.cs
namespace CRM.Application.Sla.DTOs;

public record SlaPolicyDto(
    Guid Id,
    Guid? DepartmentId,
    string Priority,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent);
```

- [ ] **Step 4: Implement query and handler**

```csharp
// src/CRM.Application/Sla/Queries/ListSlaPoliciesQuery.cs
using CRM.Application.Sla.DTOs;
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Queries;

public record ListSlaPoliciesQuery : IRequest<IReadOnlyList<SlaPolicyDto>>;

public class ListSlaPoliciesQueryHandler
    : IRequestHandler<ListSlaPoliciesQuery, IReadOnlyList<SlaPolicyDto>>
{
    private readonly ISlaPolicyRepository _policies;

    public ListSlaPoliciesQueryHandler(ISlaPolicyRepository policies)
        => _policies = policies;

    public async Task<IReadOnlyList<SlaPolicyDto>> Handle(
        ListSlaPoliciesQuery query, CancellationToken ct)
    {
        var policies = await _policies.ListAllAsync(ct);
        return policies
            .Select(p => new SlaPolicyDto(
                p.Id, p.DepartmentId, p.Priority.ToString(),
                p.FirstResponseMinutes, p.ResolutionMinutes,
                p.WarningThresholdPercent, p.BreachThresholdPercent,
                p.CriticalBreachThresholdPercent))
            .ToList();
    }
}
```

- [ ] **Step 5: Implement create command**

```csharp
// src/CRM.Application/Sla/Commands/CreateSlaPolicyCommand.cs
using CRM.Domain.Sla;
using CRM.Domain.Tickets.Enums;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record CreateSlaPolicyCommand(
    TicketPriority Priority,
    Guid? DepartmentId,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent) : IRequest<Guid>;

public class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, Guid>
{
    private readonly ISlaPolicyRepository _policies;

    public CreateSlaPolicyCommandHandler(ISlaPolicyRepository policies)
        => _policies = policies;

    public async Task<Guid> Handle(CreateSlaPolicyCommand cmd, CancellationToken ct)
    {
        var errors = new List<ValidationFailure>();

        if (cmd.FirstResponseMinutes <= 0 || cmd.FirstResponseMinutes >= cmd.ResolutionMinutes)
            errors.Add(new ValidationFailure(nameof(cmd.FirstResponseMinutes),
                "First response minutes must be > 0 and < resolution minutes."));

        if (cmd.WarningThresholdPercent >= cmd.BreachThresholdPercent)
            errors.Add(new ValidationFailure(nameof(cmd.WarningThresholdPercent),
                "Warning threshold must be less than breach threshold."));

        if (cmd.BreachThresholdPercent >= cmd.CriticalBreachThresholdPercent)
            errors.Add(new ValidationFailure(nameof(cmd.BreachThresholdPercent),
                "Breach threshold must be less than critical breach threshold."));

        if (errors.Any()) throw new ValidationException(errors);

        var policy = SlaPolicy.Create(
            cmd.Priority, cmd.FirstResponseMinutes, cmd.ResolutionMinutes,
            cmd.DepartmentId, cmd.WarningThresholdPercent,
            cmd.BreachThresholdPercent, cmd.CriticalBreachThresholdPercent);

        await _policies.AddAsync(policy, ct);
        await _policies.SaveChangesAsync(ct);

        return policy.Id;
    }
}
```

- [ ] **Step 6: Implement update command**

```csharp
// src/CRM.Application/Sla/Commands/UpdateSlaPolicyCommand.cs
using CRM.Domain.Sla;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record UpdateSlaPolicyCommand(
    Guid PolicyId,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent) : IRequest;

public class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand>
{
    private readonly ISlaPolicyRepository _policies;

    public UpdateSlaPolicyCommandHandler(ISlaPolicyRepository policies)
        => _policies = policies;

    public async Task Handle(UpdateSlaPolicyCommand cmd, CancellationToken ct)
    {
        var policy = await _policies.FindByIdAsync(cmd.PolicyId, ct)
            ?? throw new KeyNotFoundException($"SLA Policy {cmd.PolicyId} not found.");

        var errors = new List<ValidationFailure>();

        if (cmd.FirstResponseMinutes <= 0 || cmd.FirstResponseMinutes >= cmd.ResolutionMinutes)
            errors.Add(new ValidationFailure(nameof(cmd.FirstResponseMinutes),
                "First response minutes must be > 0 and < resolution minutes."));

        if (cmd.WarningThresholdPercent >= cmd.BreachThresholdPercent)
            errors.Add(new ValidationFailure(nameof(cmd.WarningThresholdPercent),
                "Warning threshold must be less than breach threshold."));

        if (cmd.BreachThresholdPercent >= cmd.CriticalBreachThresholdPercent)
            errors.Add(new ValidationFailure(nameof(cmd.BreachThresholdPercent),
                "Breach threshold must be less than critical breach threshold."));

        if (errors.Any()) throw new ValidationException(errors);

        policy.Update(
            cmd.FirstResponseMinutes, cmd.ResolutionMinutes,
            cmd.WarningThresholdPercent, cmd.BreachThresholdPercent,
            cmd.CriticalBreachThresholdPercent);

        await _policies.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SlaPolicyCrudTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Commit application layer**

```bash
git add src/CRM.Application/Sla/ \
        tests/CRM.Application.Tests/Sla/SlaPolicyCrudTests.cs
git commit -m "feat(sla): add SLA policy CRUD commands with threshold validation"
```

---

## Task 2: SlaPoliciesController

**Files:**
- Create: `src/CRM.API/Controllers/Admin/SlaPoliciesController.cs`
- Test: `tests/CRM.API.Tests/Admin/SlaPoliciesControllerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Admin/SlaPoliciesControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Sla.Commands;
using CRM.Application.Sla.DTOs;
using CRM.Application.Sla.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class SlaPoliciesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Admin")
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
    public async Task ListPolicies_Returns200WithList()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListSlaPoliciesQuery>(), default))
                 .ReturnsAsync(new List<SlaPolicyDto>
                 {
                     new(Guid.NewGuid(), null, "High", 60, 480, 80, 100, 200)
                 });

        var client = BuildClient("Manager");
        var response = await client.GetAsync("/api/admin/sla/policies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreatePolicy_AdminRole_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateSlaPolicyCommand>(), default))
                 .ReturnsAsync(Guid.NewGuid());

        var client = BuildClient("Admin");
        var response = await client.PostAsJsonAsync("/api/admin/sla/policies", new
        {
            priority = "High",
            firstResponseMinutes = 60,
            resolutionMinutes = 480,
            warningThresholdPercent = 80,
            breachThresholdPercent = 100,
            criticalBreachThresholdPercent = 200
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePolicy_AdminRole_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateSlaPolicyCommand>(), default))
                 .Returns(Task.CompletedTask);

        var client = BuildClient("Admin");
        var response = await client.PutAsJsonAsync($"/api/admin/sla/policies/{Guid.NewGuid()}", new
        {
            firstResponseMinutes = 90,
            resolutionMinutes = 600,
            warningThresholdPercent = 75,
            breachThresholdPercent = 100,
            criticalBreachThresholdPercent = 150
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "SlaPoliciesControllerTests" -v n
```

Expected: FAIL — controller does not exist.

- [ ] **Step 3: Create SlaPoliciesController**

```csharp
// src/CRM.API/Controllers/Admin/SlaPoliciesController.cs
using CRM.Application.Sla.Commands;
using CRM.Application.Sla.Queries;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers.Admin;

[ApiController]
[Route("api/admin/sla/policies")]
[Authorize(Roles = "Admin,Manager")]
public class SlaPoliciesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlaPoliciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListSlaPoliciesQuery(), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSlaPolicyRequest req, CancellationToken ct)
    {
        Enum.TryParse<TicketPriority>(req.Priority, out var priority);
        var id = await _mediator.Send(new CreateSlaPolicyCommand(
            priority, req.DepartmentId, req.FirstResponseMinutes, req.ResolutionMinutes,
            req.WarningThresholdPercent, req.BreachThresholdPercent,
            req.CriticalBreachThresholdPercent), ct);
        return StatusCode(201, new { id });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSlaPolicyRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateSlaPolicyCommand(
                id, req.FirstResponseMinutes, req.ResolutionMinutes,
                req.WarningThresholdPercent, req.BreachThresholdPercent,
                req.CriticalBreachThresholdPercent), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record CreateSlaPolicyRequest(
    string Priority,
    Guid? DepartmentId,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent);

public record UpdateSlaPolicyRequest(
    int FirstResponseMinutes,
    int ResolutionMinutes,
    int WarningThresholdPercent,
    int BreachThresholdPercent,
    int CriticalBreachThresholdPercent);
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "SlaPoliciesControllerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/Admin/SlaPoliciesController.cs \
        tests/CRM.API.Tests/Admin/SlaPoliciesControllerTests.cs
git commit -m "feat(api): add GET/POST/PUT /api/admin/sla/policies endpoints"
```
