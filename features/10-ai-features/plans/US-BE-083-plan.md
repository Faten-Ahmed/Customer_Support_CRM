# AI Ticket Summarization — Implementation Plan

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

**Story:** US-BE-083  
**Goal:** Implement `POST /api/ai/tickets/{id}/summarize` — generates a 2–4 sentence summary of a ticket's full message thread using Azure OpenAI. Ticket with zero messages returns 422 `NO_MESSAGES_TO_PROCESS`. Summary is NOT persisted. Azure 30s timeout → 503 `AI_PROVIDER_UNAVAILABLE`. Response includes `modelUsed` field.

**Architecture:** `SummarizeTicketQuery(TicketId, RequestingUserId)` → fetches messages via `ITicketRepository.GetMessagesForAiAsync()` → calls `IAzureOpenAiService.SummarizeAsync(messages)`. Azure service validated at startup for UAE/Europe region. `AiController` at `/api/ai`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Azure OpenAI SDK, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/AI/IAzureOpenAiService.cs` |
| Create | `src/CRM.Application/AI/DTOs/AiSummaryDto.cs` |
| Create | `src/CRM.Application/AI/Queries/SummarizeTicketQuery.cs` |
| Create | `src/CRM.API/Controllers/AiController.cs` |
| Test   | `tests/CRM.Application.Tests/AI/SummarizeTicketQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/AI/AiControllerTests.cs` |

---

## Task 1: AI Ticket Summarization

> Note: `ITicketRepository` and `Ticket` entity are from US-BE-019. Message retrieval method added here. Implement US-BE-019 first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/AI/SummarizeTicketQueryHandlerTests.cs
using CRM.Application.AI.Queries;
using CRM.Domain.AI;
using CRM.Domain.Tickets;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.AI;

public class SummarizeTicketQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAzureOpenAiService> _ai = new();
    private readonly SummarizeTicketQueryHandler _handler;

    public SummarizeTicketQueryHandlerTests()
    {
        _handler = new SummarizeTicketQueryHandler(_tickets.Object, _ai.Object);
    }

    [Fact]
    public async Task Handle_TicketWithMessages_ReturnsSummary()
    {
        var ticketId = Guid.NewGuid();
        var messages = new List<TicketMessageContext>
        {
            new("Customer", "I cannot log in to my account.", DateTime.UtcNow.AddHours(-2)),
            new("Agent", "Have you tried resetting your password?", DateTime.UtcNow.AddHours(-1))
        };

        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default)).ReturnsAsync(messages);
        _ai.Setup(a => a.SummarizeAsync(messages, default))
           .ReturnsAsync(new AiTextResult(
               "Customer reported login issue. Agent suggested password reset.",
               "gpt-4o-mini"));

        var result = await _handler.Handle(
            new SummarizeTicketQuery(ticketId, Guid.NewGuid()), default);

        Assert.Contains("login", result.Summary);
        Assert.Equal("gpt-4o-mini", result.ModelUsed);
    }

    [Fact]
    public async Task Handle_NoMessages_ThrowsValidationException()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default))
                .ReturnsAsync(new List<TicketMessageContext>());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(new SummarizeTicketQuery(ticketId, Guid.NewGuid()), default));

        Assert.Contains("NO_MESSAGES_TO_PROCESS", ex.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task Handle_AzureTimeout_ThrowsAiProviderException()
    {
        var ticketId = Guid.NewGuid();
        var messages = new List<TicketMessageContext>
        {
            new("Customer", "Help!", DateTime.UtcNow)
        };
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default)).ReturnsAsync(messages);
        _ai.Setup(a => a.SummarizeAsync(messages, default))
           .ThrowsAsync(new AiProviderException("AI_PROVIDER_UNAVAILABLE"));

        await Assert.ThrowsAsync<AiProviderException>(() =>
            _handler.Handle(new SummarizeTicketQuery(ticketId, Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SummarizeTicketQueryHandlerTests" -v n
```

Expected: FAIL — `IAzureOpenAiService` does not exist yet.

- [ ] **Step 3: Create IAzureOpenAiService and domain types**

```csharp
// src/CRM.Domain/AI/IAzureOpenAiService.cs
namespace CRM.Domain.AI;

public record TicketMessageContext(string SenderRole, string Body, DateTime SentAt);

public record AiTextResult(string Text, string ModelUsed);

public record AiCategorySuggestion(
    Guid CategoryId, string CategoryName, string? ParentCategoryName,
    double Confidence, string ConfidenceBand, string Label);

public class AiProviderException : Exception
{
    public AiProviderException(string message) : base(message) { }
}

public interface IAzureOpenAiService
{
    Task<AiTextResult> SummarizeAsync(
        IReadOnlyList<TicketMessageContext> messages,
        CancellationToken ct = default);

    Task<AiTextResult> SuggestReplyAsync(
        IReadOnlyList<TicketMessageContext> messages,
        string tone, string language,
        CancellationToken ct = default);

    Task<IReadOnlyList<AiCategorySuggestion>> SuggestCategoriesAsync(
        string ticketSubject, string ticketBody,
        IReadOnlyList<string> activeCategories,
        CancellationToken ct = default);

    Task<AiTextResult> SuggestArticlesAsync(
        string ticketBody,
        IReadOnlyList<string> articleTitles,
        CancellationToken ct = default);
}
```

- [ ] **Step 4: Add GetMessagesForAiAsync to ITicketRepository**

Open `src/CRM.Domain/Tickets/ITicketRepository.cs` and add:

```csharp
Task<IReadOnlyList<TicketMessageContext>> GetMessagesForAiAsync(
    Guid ticketId, CancellationToken ct = default);
```

- [ ] **Step 5: Create AiSummaryDto**

```csharp
// src/CRM.Application/AI/DTOs/AiSummaryDto.cs
namespace CRM.Application.AI.DTOs;

public record AiSummaryDto(string Summary, string ModelUsed);
```

- [ ] **Step 6: Implement SummarizeTicketQuery**

```csharp
// src/CRM.Application/AI/Queries/SummarizeTicketQuery.cs
using CRM.Application.AI.DTOs;
using CRM.Domain.AI;
using CRM.Domain.Tickets;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.AI.Queries;

public record SummarizeTicketQuery(Guid TicketId, Guid RequestingUserId)
    : IRequest<AiSummaryDto>;

public class SummarizeTicketQueryHandler
    : IRequestHandler<SummarizeTicketQuery, AiSummaryDto>
{
    private readonly ITicketRepository _tickets;
    private readonly IAzureOpenAiService _ai;

    public SummarizeTicketQueryHandler(ITicketRepository tickets, IAzureOpenAiService ai)
    {
        _tickets = tickets;
        _ai = ai;
    }

    public async Task<AiSummaryDto> Handle(SummarizeTicketQuery query, CancellationToken ct)
    {
        var messages = await _tickets.GetMessagesForAiAsync(query.TicketId, ct);

        if (messages.Count == 0)
            throw new ValidationException(new[]
            {
                new ValidationFailure("Messages",
                    "Ticket has no messages to summarize.", "NO_MESSAGES_TO_PROCESS")
            });

        var result = await _ai.SummarizeAsync(messages, ct);
        return new AiSummaryDto(result.Text, result.ModelUsed);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SummarizeTicketQueryHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 8: Create AiController**

```csharp
// src/CRM.API/Controllers/AiController.cs
using CRM.Application.AI.Queries;
using CRM.Domain.AI;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public AiController(IMediator mediator) => _mediator = mediator;

    [HttpPost("tickets/{id:guid}/summarize")]
    public async Task<IActionResult> Summarize(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new SummarizeTicketQuery(id, CurrentUserId), ct);
            return Ok(new { data = result });
        }
        catch (FluentValidation.ValidationException ex)
            { return UnprocessableEntity(new { error = ex.Errors.First().ErrorCode }); }
        catch (AiProviderException ex)
            { return StatusCode(503, new { error = ex.Message }); }
        catch (KeyNotFoundException ex)
            { return NotFound(new { error = ex.Message }); }
    }
}
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/AI/AiControllerTests.cs
using System.Net;
using CRM.Application.AI.DTOs;
using CRM.Application.AI.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.AI;

public class AiControllerTests
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
    public async Task Summarize_ValidTicket_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SummarizeTicketQuery>(), default))
                 .ReturnsAsync(new AiSummaryDto(
                     "Customer had login issues. Agent resolved it.", "gpt-4o-mini"));

        var response = await BuildClient()
            .PostAsync($"/api/ai/tickets/{Guid.NewGuid()}/summarize", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Summarize_NoMessages_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SummarizeTicketQuery>(), default))
                 .ThrowsAsync(new FluentValidation.ValidationException(
                     new[] { new FluentValidation.Results.ValidationFailure(
                         "Messages", "No messages.", "NO_MESSAGES_TO_PROCESS") }));

        var response = await BuildClient()
            .PostAsync($"/api/ai/tickets/{Guid.NewGuid()}/summarize", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AiControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Domain/AI/ \
        src/CRM.Application/AI/ \
        src/CRM.API/Controllers/AiController.cs \
        tests/CRM.Application.Tests/AI/SummarizeTicketQueryHandlerTests.cs \
        tests/CRM.API.Tests/AI/AiControllerTests.cs
git commit -m "feat(ai): add POST /api/ai/tickets/{id}/summarize — Azure OpenAI thread summary, not persisted"
```
