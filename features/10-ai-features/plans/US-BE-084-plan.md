# AI Suggest Reply — Implementation Plan

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

**Story:** US-BE-084  
**Goal:** Implement `POST /api/ai/tickets/{id}/suggest-reply` — generates a suggested reply draft using Azure OpenAI. Accepts `tone` (professional/friendly/formal) and `language` (en/ar). Uses last 5 messages as context, last customer message as primary target. Not persisted; agent must manually use it. Invalid `tone` or `language` returns 422. Ticket with zero messages returns 422 `NO_MESSAGES_TO_PROCESS`.

**Architecture:** `SuggestReplyQuery(TicketId, RequestingUserId, Tone, Language)` → fetches last 5 messages → calls `IAzureOpenAiService.SuggestReplyAsync(messages, tone, language)`. Arabic responses use `gpt-4o` deployment (configurable); English uses `gpt-4o-mini`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Azure OpenAI SDK, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/AI/DTOs/AiSuggestReplyDto.cs` |
| Create | `src/CRM.Application/AI/Queries/SuggestReplyQuery.cs` |
| Modify | `src/CRM.API/Controllers/AiController.cs` |
| Test   | `tests/CRM.Application.Tests/AI/SuggestReplyQueryHandlerTests.cs` |

---

## Task 1: AI Suggest Reply Query

> Note: `IAzureOpenAiService` and `ITicketRepository.GetMessagesForAiAsync` are from US-BE-083. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/AI/SuggestReplyQueryHandlerTests.cs
using CRM.Application.AI.Queries;
using CRM.Domain.AI;
using CRM.Domain.Tickets;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.AI;

public class SuggestReplyQueryHandlerTests
{
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IAzureOpenAiService> _ai = new();
    private readonly SuggestReplyQueryHandler _handler;

    public SuggestReplyQueryHandlerTests()
    {
        _handler = new SuggestReplyQueryHandler(_tickets.Object, _ai.Object);
    }

    [Fact]
    public async Task Handle_ValidToneAndLanguage_ReturnsSuggestedReply()
    {
        var ticketId = Guid.NewGuid();
        var messages = new List<TicketMessageContext>
        {
            new("Customer", "I cannot access my account.", DateTime.UtcNow)
        };
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default)).ReturnsAsync(messages);
        _ai.Setup(a => a.SuggestReplyAsync(messages, "professional", "en", default))
           .ReturnsAsync(new AiTextResult(
               "Thank you for reaching out. Let me help you regain access.", "gpt-4o-mini"));

        var result = await _handler.Handle(
            new SuggestReplyQuery(ticketId, Guid.NewGuid(), "professional", "en"),
            default);

        Assert.Contains("Thank you", result.SuggestedReply);
        Assert.Equal("gpt-4o-mini", result.ModelUsed);
    }

    [Fact]
    public async Task Handle_ArabicLanguage_PassesCorrectLanguageToService()
    {
        var ticketId = Guid.NewGuid();
        var messages = new List<TicketMessageContext>
        {
            new("Customer", "مرحبا", DateTime.UtcNow)
        };
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default)).ReturnsAsync(messages);
        _ai.Setup(a => a.SuggestReplyAsync(messages, "professional", "ar", default))
           .ReturnsAsync(new AiTextResult("أهلاً بك", "gpt-4o"));

        var result = await _handler.Handle(
            new SuggestReplyQuery(ticketId, Guid.NewGuid(), "professional", "ar"),
            default);

        Assert.Equal("gpt-4o", result.ModelUsed);
        _ai.Verify(a => a.SuggestReplyAsync(messages, "professional", "ar", default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidTone_ThrowsValidationException()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default))
                .ReturnsAsync(new List<TicketMessageContext>
                {
                    new("Customer", "Help!", DateTime.UtcNow)
                });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new SuggestReplyQuery(ticketId, Guid.NewGuid(), "casual", "en"),
                default));

        Assert.Contains("INVALID_TONE", ex.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task Handle_InvalidLanguage_ThrowsValidationException()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default))
                .ReturnsAsync(new List<TicketMessageContext>
                {
                    new("Customer", "Help!", DateTime.UtcNow)
                });

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new SuggestReplyQuery(ticketId, Guid.NewGuid(), "professional", "fr"),
                default));

        Assert.Contains("INVALID_LANGUAGE", ex.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task Handle_NoMessages_ThrowsValidationException()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.GetMessagesForAiAsync(ticketId, default))
                .ReturnsAsync(new List<TicketMessageContext>());

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _handler.Handle(
                new SuggestReplyQuery(ticketId, Guid.NewGuid(), "professional", "en"),
                default));

        Assert.Contains("NO_MESSAGES_TO_PROCESS", ex.Errors.First().ErrorCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SuggestReplyQueryHandlerTests" -v n
```

Expected: FAIL — `SuggestReplyQuery` does not exist yet.

- [ ] **Step 3: Create AiSuggestReplyDto**

```csharp
// src/CRM.Application/AI/DTOs/AiSuggestReplyDto.cs
namespace CRM.Application.AI.DTOs;

public record AiSuggestReplyDto(string SuggestedReply, string ModelUsed);
```

- [ ] **Step 4: Implement SuggestReplyQuery**

```csharp
// src/CRM.Application/AI/Queries/SuggestReplyQuery.cs
using CRM.Application.AI.DTOs;
using CRM.Domain.AI;
using CRM.Domain.Tickets;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.AI.Queries;

public record SuggestReplyQuery(
    Guid TicketId,
    Guid RequestingUserId,
    string Tone,
    string Language) : IRequest<AiSuggestReplyDto>;

public class SuggestReplyQueryHandler
    : IRequestHandler<SuggestReplyQuery, AiSuggestReplyDto>
{
    private static readonly string[] ValidTones = ["professional", "friendly", "formal"];
    private static readonly string[] ValidLanguages = ["en", "ar"];

    private readonly ITicketRepository _tickets;
    private readonly IAzureOpenAiService _ai;

    public SuggestReplyQueryHandler(ITicketRepository tickets, IAzureOpenAiService ai)
    {
        _tickets = tickets;
        _ai = ai;
    }

    public async Task<AiSuggestReplyDto> Handle(
        SuggestReplyQuery query, CancellationToken ct)
    {
        var errors = new List<ValidationFailure>();

        if (!ValidTones.Contains(query.Tone))
            errors.Add(new ValidationFailure("Tone",
                $"Tone must be one of: {string.Join(", ", ValidTones)}.", "INVALID_TONE"));

        if (!ValidLanguages.Contains(query.Language))
            errors.Add(new ValidationFailure("Language",
                "Language must be 'en' or 'ar'.", "INVALID_LANGUAGE"));

        if (errors.Count > 0) throw new ValidationException(errors);

        var allMessages = await _tickets.GetMessagesForAiAsync(query.TicketId, ct);

        if (allMessages.Count == 0)
            throw new ValidationException(new[]
            {
                new ValidationFailure("Messages",
                    "Ticket has no messages to process.", "NO_MESSAGES_TO_PROCESS")
            });

        // Last 5 messages as context
        var context = allMessages.TakeLast(5).ToList();
        var result = await _ai.SuggestReplyAsync(context, query.Tone, query.Language, ct);
        return new AiSuggestReplyDto(result.Text, result.ModelUsed);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "SuggestReplyQueryHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 6: Add SuggestReply action to AiController**

Open `src/CRM.API/Controllers/AiController.cs` and add:

```csharp
[HttpPost("tickets/{id:guid}/suggest-reply")]
public async Task<IActionResult> SuggestReply(
    Guid id, [FromBody] SuggestReplyRequest req, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new SuggestReplyQuery(id, CurrentUserId, req.Tone, req.Language), ct);
        return Ok(new { data = result });
    }
    catch (FluentValidation.ValidationException ex)
        { return UnprocessableEntity(new { errors = ex.Errors.Select(e => e.ErrorCode) }); }
    catch (AiProviderException ex)
        { return StatusCode(503, new { error = ex.Message }); }
}
```

Add the request record:

```csharp
public record SuggestReplyRequest(string Tone, string Language);
```

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/AI/DTOs/AiSuggestReplyDto.cs \
        src/CRM.Application/AI/Queries/SuggestReplyQuery.cs \
        src/CRM.API/Controllers/AiController.cs \
        tests/CRM.Application.Tests/AI/SuggestReplyQueryHandlerTests.cs
git commit -m "feat(ai): add POST /api/ai/tickets/{id}/suggest-reply — tone/language-aware draft with Arabic gpt-4o routing"
```
