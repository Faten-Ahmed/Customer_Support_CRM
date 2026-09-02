# Render Quick-Reply Template — Implementation Plan

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

**Story:** US-BE-061  
**Goal:** Implement `POST /api/agents/me/templates/{id}/render` — takes a `ticketId`, loads the template and ticket context data (customer name, agent name, ticket number, department), performs `{{token}}` substitution, and returns the rendered string. Unknown tokens are left as-is (BR-AGT-015).

**Architecture:** `RenderTemplateQuery(TemplateId, TicketId, AgentId)` → loads template, loads ticket + joins customer + agent + department, runs token substitution. Supported tokens: `{{customer_name}}`, `{{agent_name}}`, `{{ticket_number}}`, `{{department}}`. Returns the rendered string.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Agents/Queries/RenderTemplateQuery.cs` |
| Modify | `src/CRM.API/Controllers/AgentMeController.cs` |
| Test   | `tests/CRM.Application.Tests/Agents/RenderTemplateQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Agents/AgentMeControllerRenderTemplateTests.cs` |

---

## Task 1: RenderTemplate Query + Endpoint

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Agents/RenderTemplateQueryHandlerTests.cs
using CRM.Application.Agents.Queries;
using CRM.Domain.Templates;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class RenderTemplateQueryHandlerTests
{
    private readonly Mock<IQuickReplyTemplateRepository> _templates = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly RenderTemplateQueryHandler _handler;

    public RenderTemplateQueryHandlerTests()
    {
        _handler = new RenderTemplateQueryHandler(
            _templates.Object, _tickets.Object, _users.Object);
    }

    [Fact]
    public async Task Handle_AllTokens_SubstitutesCorrectly()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Support",
            "Hello {{customer_name}}, your ticket {{ticket_number}} from {{department}} " +
            "is handled by {{agent_name}}.",
            "Greeting", agentId);

        var ticketContext = new TicketRenderContext(
            "TKT-2025-00043", "Sara Al-Mansouri", "Ahmed Hassan", "IT Support");

        _templates.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);
        _tickets.Setup(r => r.GetRenderContextAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(ticketContext);

        var result = await _handler.Handle(
            new RenderTemplateQuery(template.Id, Guid.NewGuid(), agentId), default);

        Assert.Equal(
            "Hello Sara Al-Mansouri, your ticket TKT-2025-00043 from IT Support " +
            "is handled by Ahmed Hassan.",
            result);
    }

    [Fact]
    public async Task Handle_UnknownToken_LeavesAsIs()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Custom", "Hello {{customer_name}} and {{unknown_token}}.", "Cat", agentId);

        var ticketContext = new TicketRenderContext(
            "TKT-001", "Sara", "Ahmed", "IT");

        _templates.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);
        _tickets.Setup(r => r.GetRenderContextAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync(ticketContext);

        var result = await _handler.Handle(
            new RenderTemplateQuery(template.Id, Guid.NewGuid(), agentId), default);

        Assert.Contains("{{unknown_token}}", result);
        Assert.Contains("Sara", result);
    }

    [Fact]
    public async Task Handle_TemplateNotFound_ThrowsKeyNotFoundException()
    {
        _templates.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                  .ReturnsAsync((QuickReplyTemplate?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new RenderTemplateQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
                default));
    }

    [Fact]
    public async Task Handle_TicketNotFound_ThrowsKeyNotFoundException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "T", "Content", null, agentId);
        _templates.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);
        _tickets.Setup(r => r.GetRenderContextAsync(It.IsAny<Guid>(), default))
                .ReturnsAsync((TicketRenderContext?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new RenderTemplateQuery(template.Id, Guid.NewGuid(), agentId),
                default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RenderTemplateQueryHandlerTests" -v n
```

Expected: FAIL — `RenderTemplateQuery` and `TicketRenderContext` do not exist yet.

- [ ] **Step 3: Add GetRenderContextAsync to ITicketRepository**

Add to `src/CRM.Domain/Tickets/ITicketRepository.cs`:

```csharp
public record TicketRenderContext(
    string TicketNumber,
    string CustomerFullName,
    string AgentFullName,
    string DepartmentName);

Task<TicketRenderContext?> GetRenderContextAsync(
    Guid ticketId, CancellationToken ct = default);
```

- [ ] **Step 4: Implement RenderTemplateQuery**

```csharp
// src/CRM.Application/Agents/Queries/RenderTemplateQuery.cs
using CRM.Domain.Templates;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record RenderTemplateQuery(Guid TemplateId, Guid TicketId, Guid AgentId)
    : IRequest<string>;

public class RenderTemplateQueryHandler : IRequestHandler<RenderTemplateQuery, string>
{
    private readonly IQuickReplyTemplateRepository _templates;
    private readonly ITicketRepository _tickets;
    private readonly IUserRepository _users;

    public RenderTemplateQueryHandler(
        IQuickReplyTemplateRepository templates,
        ITicketRepository tickets,
        IUserRepository users)
    {
        _templates = templates;
        _tickets = tickets;
        _users = users;
    }

    public async Task<string> Handle(RenderTemplateQuery query, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(query.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {query.TemplateId} not found.");

        var context = await _tickets.GetRenderContextAsync(query.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {query.TicketId} not found.");

        var rendered = template.Content
            .Replace("{{customer_name}}", context.CustomerFullName)
            .Replace("{{agent_name}}", context.AgentFullName)
            .Replace("{{ticket_number}}", context.TicketNumber)
            .Replace("{{department}}", context.DepartmentName);

        return rendered;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "RenderTemplateQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 6: Add render endpoint to AgentMeController**

```csharp
// Add to src/CRM.API/Controllers/AgentMeController.cs:

[HttpPost("templates/{id:guid}/render")]
public async Task<IActionResult> RenderTemplate(
    Guid id, [FromBody] RenderTemplateRequest req, CancellationToken ct)
{
    try
    {
        var rendered = await _mediator.Send(
            new RenderTemplateQuery(id, req.TicketId, CurrentUserId), ct);
        return Ok(new { data = new { rendered } });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
}

public record RenderTemplateRequest(Guid TicketId);
```

- [ ] **Step 7: Write controller test**

```csharp
// tests/CRM.API.Tests/Agents/AgentMeControllerRenderTemplateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Agents.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Agents;

public class AgentMeControllerRenderTemplateTests
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
    public async Task RenderTemplate_Returns200WithRenderedContent()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RenderTemplateQuery>(), default))
                 .ReturnsAsync("Hello Sara, your ticket TKT-001 is being processed.");

        var response = await BuildClient().PostAsJsonAsync(
            $"/api/agents/me/templates/{Guid.NewGuid()}/render",
            new { ticketId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RenderTemplate_TicketNotFound_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RenderTemplateQuery>(), default))
                 .ThrowsAsync(new KeyNotFoundException("Ticket not found."));

        var response = await BuildClient().PostAsJsonAsync(
            $"/api/agents/me/templates/{Guid.NewGuid()}/render",
            new { ticketId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 8: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AgentMeControllerRenderTemplateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Application/Agents/Queries/RenderTemplateQuery.cs \
        src/CRM.API/Controllers/AgentMeController.cs \
        tests/CRM.Application.Tests/Agents/RenderTemplateQueryHandlerTests.cs \
        tests/CRM.API.Tests/Agents/AgentMeControllerRenderTemplateTests.cs
git commit -m "feat(agents): add POST /api/agents/me/templates/{id}/render with token substitution and unknown-token pass-through"
```
