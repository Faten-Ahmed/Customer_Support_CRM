# Update Agent Availability — Implementation Plan

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

**Story:** US-BE-059  
**Goal:** Implement `PUT /api/agents/me/availability` — allows an agent to set their own availability status to `Available`, `Busy`, `Away`, or `Offline`. Only `Available` agents receive auto-assigned tickets (enforced by US-BE-035 AutoAssignTicketJob).

**Architecture:** `UpdateAvailabilityCommand(AgentId, AvailabilityStatus)` → loads user, calls `user.SetAvailability(status)`, saves. `AvailabilityStatus` is an enum added to the `User` entity. Returns updated status + timestamp. Invalid string → 400.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Users/AvailabilityStatus.cs` |
| Create | `src/CRM.Application/Agents/Commands/UpdateAvailabilityCommand.cs` |
| Modify | `src/CRM.Domain/Users/User.cs` |
| Modify | `src/CRM.API/Controllers/AgentMeController.cs` |
| Test   | `tests/CRM.Application.Tests/Agents/UpdateAvailabilityCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Agents/AgentMeControllerAvailabilityTests.cs` |

---

## Task 1: UpdateAvailability Command + Controller

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Agents/UpdateAvailabilityCommandHandlerTests.cs
using CRM.Application.Agents.Commands;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class UpdateAvailabilityCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repo = new();
    private readonly UpdateAvailabilityCommandHandler _handler;

    public UpdateAvailabilityCommandHandlerTests()
    {
        _handler = new UpdateAvailabilityCommandHandler(_repo.Object);
    }

    [Theory]
    [InlineData(AvailabilityStatus.Available)]
    [InlineData(AvailabilityStatus.Busy)]
    [InlineData(AvailabilityStatus.Away)]
    [InlineData(AvailabilityStatus.Offline)]
    public async Task Handle_ValidStatus_UpdatesUser(AvailabilityStatus status)
    {
        var user = User.CreateInternal(
            Guid.NewGuid(), "Agent", "One", "agent@test.com", UserRole.Agent);
        _repo.Setup(r => r.FindByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _handler.Handle(
            new UpdateAvailabilityCommand(user.Id, status), default);

        Assert.Equal(status.ToString(), result.AvailabilityStatus);
        Assert.NotNull(result.LastAvailabilityChange);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new UpdateAvailabilityCommand(Guid.NewGuid(), AvailabilityStatus.Available),
                default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UpdateAvailabilityCommandHandlerTests" -v n
```

Expected: FAIL — `AvailabilityStatus` and `UpdateAvailabilityCommand` do not exist yet.

- [ ] **Step 3: Create AvailabilityStatus enum**

```csharp
// src/CRM.Domain/Users/AvailabilityStatus.cs
namespace CRM.Domain.Users;

public enum AvailabilityStatus
{
    Available,
    Busy,
    Away,
    Offline
}
```

- [ ] **Step 4: Add availability fields to User entity**

Add to `src/CRM.Domain/Users/User.cs`:

```csharp
public AvailabilityStatus AvailabilityStatus { get; private set; } = AvailabilityStatus.Offline;
public DateTime? LastAvailabilityChange { get; private set; }

public void SetAvailability(AvailabilityStatus status)
{
    AvailabilityStatus = status;
    LastAvailabilityChange = DateTime.UtcNow;
}
```

- [ ] **Step 5: Implement UpdateAvailabilityCommand**

```csharp
// src/CRM.Application/Agents/Commands/UpdateAvailabilityCommand.cs
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record UpdateAvailabilityCommand(
    Guid AgentId,
    AvailabilityStatus Status) : IRequest<AvailabilityResult>;

public record AvailabilityResult(
    Guid Id,
    string AvailabilityStatus,
    DateTime? LastAvailabilityChange);

public class UpdateAvailabilityCommandHandler
    : IRequestHandler<UpdateAvailabilityCommand, AvailabilityResult>
{
    private readonly IUserRepository _users;

    public UpdateAvailabilityCommandHandler(IUserRepository users) => _users = users;

    public async Task<AvailabilityResult> Handle(
        UpdateAvailabilityCommand cmd, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(cmd.AgentId, ct)
            ?? throw new KeyNotFoundException($"User {cmd.AgentId} not found.");

        user.SetAvailability(cmd.Status);
        await _users.SaveChangesAsync(ct);

        return new AvailabilityResult(
            user.Id,
            user.AvailabilityStatus.ToString(),
            user.LastAvailabilityChange);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UpdateAvailabilityCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 7: Add endpoint to AgentMeController**

```csharp
// Add to src/CRM.API/Controllers/AgentMeController.cs:

[HttpPut("availability")]
public async Task<IActionResult> UpdateAvailability(
    [FromBody] UpdateAvailabilityRequest req, CancellationToken ct)
{
    if (!Enum.TryParse<AvailabilityStatus>(req.Status, out var status))
        return BadRequest(new { error = $"Invalid status '{req.Status}'. Valid values: Available, Busy, Away, Offline." });

    var result = await _mediator.Send(
        new UpdateAvailabilityCommand(CurrentUserId, status), ct);

    return Ok(new { data = result });
}

public record UpdateAvailabilityRequest(string Status);
```

Add `using CRM.Application.Agents.Commands;` and `using CRM.Domain.Users;` to the controller.

- [ ] **Step 8: Write controller test**

```csharp
// tests/CRM.API.Tests/Agents/AgentMeControllerAvailabilityTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Agents.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Agents;

public class AgentMeControllerAvailabilityTests
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
    public async Task UpdateAvailability_ValidStatus_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateAvailabilityCommand>(), default))
                 .ReturnsAsync(new AvailabilityResult(
                     Guid.NewGuid(), "Busy", DateTime.UtcNow));

        var response = await BuildClient().PutAsJsonAsync(
            "/api/agents/me/availability", new { status = "Busy" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAvailability_InvalidStatus_Returns400()
    {
        var response = await BuildClient().PutAsJsonAsync(
            "/api/agents/me/availability", new { status = "Dancing" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 9: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AgentMeControllerAvailabilityTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Domain/Users/AvailabilityStatus.cs \
        src/CRM.Domain/Users/User.cs \
        src/CRM.Application/Agents/Commands/UpdateAvailabilityCommand.cs \
        src/CRM.API/Controllers/AgentMeController.cs \
        tests/CRM.Application.Tests/Agents/UpdateAvailabilityCommandHandlerTests.cs \
        tests/CRM.API.Tests/Agents/AgentMeControllerAvailabilityTests.cs
git commit -m "feat(agents): add PUT /api/agents/me/availability — Available/Busy/Away/Offline"
```
