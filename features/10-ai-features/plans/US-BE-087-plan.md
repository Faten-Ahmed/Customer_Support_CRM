# AI Chat Message (Portal Chatbot & Agent Assistant) — Implementation Plan

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

**Story:** US-BE-087  
**Goal:** Implement `POST /api/ai/chat/message` — sends a message to an AI chatbot. Creates `ChatSession` when `sessionId = null`. Returns `sessionId`, `reply`, `suggestedArticles[]`, `handoffRequired`, `handoffReason`. Handoff triggered by: 3 failed attempts, "human/agent" keywords, session > 10 min old and unresolved, sensitive topic. `context = "agent"` disables handoff logic. Concurrent messages on same session → 409 `SESSION_BUSY`. 30s timeout → 503.

**Architecture:** `ChatMessageCommand(SessionId?, Message, Context, RequestingUserId)` → resolves/creates `ChatSession` → appends message → calls Azure OpenAI with last 20 messages → evaluates handoff triggers → returns result. `IChatSessionRepository` for session persistence.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Azure OpenAI SDK, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Chat/ChatSession.cs` |
| Create | `src/CRM.Domain/Chat/IChatSessionRepository.cs` |
| Create | `src/CRM.Application/AI/DTOs/AiChatMessageDto.cs` |
| Create | `src/CRM.Application/AI/Commands/ChatMessageCommand.cs` |
| Modify | `src/CRM.API/Controllers/AiController.cs` |
| Test   | `tests/CRM.Application.Tests/AI/ChatMessageCommandHandlerTests.cs` |

---

## Task 1: AI Chat Message Command

> Note: `IAzureOpenAiService` and `AiProviderException` are from US-BE-083. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/AI/ChatMessageCommandHandlerTests.cs
using CRM.Application.AI.Commands;
using CRM.Domain.AI;
using CRM.Domain.Chat;
using Moq;
using Xunit;

namespace CRM.Application.Tests.AI;

public class ChatMessageCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessions = new();
    private readonly Mock<IAzureOpenAiService> _ai = new();
    private readonly ChatMessageCommandHandler _handler;

    public ChatMessageCommandHandlerTests()
    {
        _handler = new ChatMessageCommandHandler(_sessions.Object, _ai.Object);
    }

    [Fact]
    public async Task Handle_NullSessionId_CreatesNewSession()
    {
        var userId = Guid.NewGuid();
        _sessions.Setup(r => r.AddAsync(It.IsAny<ChatSession>(), default))
                 .Returns(Task.CompletedTask);
        _ai.Setup(a => a.SummarizeAsync(It.IsAny<IReadOnlyList<TicketMessageContext>>(), default))
           .ReturnsAsync(new AiTextResult("How can I help you?", "gpt-4o-mini"));

        var result = await _handler.Handle(
            new ChatMessageCommand(null, "Hello", "portal", userId), default);

        Assert.NotNull(result.SessionId);
        Assert.Equal("How can I help you?", result.Reply);
    }

    [Fact]
    public async Task Handle_ExistingSession_AppendsMessage()
    {
        var userId = Guid.NewGuid();
        var session = ChatSession.Create(userId, "portal");
        session.AddMessage("Customer", "First message");
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);
        _ai.Setup(a => a.SummarizeAsync(It.IsAny<IReadOnlyList<TicketMessageContext>>(), default))
           .ReturnsAsync(new AiTextResult("OK", "gpt-4o-mini"));

        var result = await _handler.Handle(
            new ChatMessageCommand(session.Id, "Second message", "portal", userId), default);

        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal(2, session.Messages.Count);
    }

    [Fact]
    public async Task Handle_SessionBusy_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var session = ChatSession.Create(userId, "portal");
        session.SetProcessing(true);
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ChatMessageCommand(session.Id, "Hi", "portal", userId), default));

        Assert.Contains("SESSION_BUSY", ex.Message);
    }

    [Fact]
    public async Task Handle_AgentContext_HandoffRequiredFalse()
    {
        var userId = Guid.NewGuid();
        _sessions.Setup(r => r.AddAsync(It.IsAny<ChatSession>(), default))
                 .Returns(Task.CompletedTask);
        _ai.Setup(a => a.SummarizeAsync(It.IsAny<IReadOnlyList<TicketMessageContext>>(), default))
           .ReturnsAsync(new AiTextResult("Agent response", "gpt-4o-mini"));

        var result = await _handler.Handle(
            new ChatMessageCommand(null, "How do I escalate?", "agent", userId), default);

        Assert.False(result.HandoffRequired);
    }

    [Fact]
    public async Task Handle_HumanKeyword_HandoffRequired()
    {
        var userId = Guid.NewGuid();
        var session = ChatSession.Create(userId, "portal");
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);
        _ai.Setup(a => a.SummarizeAsync(It.IsAny<IReadOnlyList<TicketMessageContext>>(), default))
           .ReturnsAsync(new AiTextResult("Connecting you now.", "gpt-4o-mini"));

        var result = await _handler.Handle(
            new ChatMessageCommand(session.Id, "I want to talk to a real person", "portal", userId),
            default);

        Assert.True(result.HandoffRequired);
        Assert.Contains("human", result.HandoffReason?.ToLower() ?? "");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChatMessageCommandHandlerTests" -v n
```

Expected: FAIL — `ChatSession` does not exist yet.

- [ ] **Step 3: Create ChatSession entity**

```csharp
// src/CRM.Domain/Chat/ChatSession.cs
namespace CRM.Domain.Chat;

public class ChatSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Context { get; private set; } = "portal";
    public string Status { get; private set; } = "Active"; // Active | HandoffPending | Closed
    public bool IsProcessing { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int FailedAttemptCount { get; private set; }
    public List<ChatSessionMessage> Messages { get; private set; } = new();

    private ChatSession() { }

    public static ChatSession Create(Guid userId, string context) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Context = context,
        Status = "Active",
        CreatedAt = DateTime.UtcNow
    };

    public void AddMessage(string senderRole, string body)
    {
        Messages.Add(new ChatSessionMessage(Guid.NewGuid(), Id, senderRole, body, DateTime.UtcNow));
    }

    public void SetProcessing(bool value) => IsProcessing = value;

    public void IncrementFailedAttempts() => FailedAttemptCount++;

    public void RequestHandoff() => Status = "HandoffPending";
}

public class ChatSessionMessage
{
    public Guid Id { get; }
    public Guid SessionId { get; }
    public string SenderRole { get; }
    public string Body { get; }
    public DateTime SentAt { get; }

    public ChatSessionMessage(Guid id, Guid sessionId, string senderRole, string body, DateTime sentAt)
    {
        Id = id;
        SessionId = sessionId;
        SenderRole = senderRole;
        Body = body;
        SentAt = sentAt;
    }
}
```

- [ ] **Step 4: Create IChatSessionRepository**

```csharp
// src/CRM.Domain/Chat/IChatSessionRepository.cs
namespace CRM.Domain.Chat;

public interface IChatSessionRepository
{
    Task<ChatSession?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ChatSession session, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 5: Create AiChatMessageDto**

```csharp
// src/CRM.Application/AI/DTOs/AiChatMessageDto.cs
namespace CRM.Application.AI.DTOs;

public record AiChatMessageDto(
    Guid SessionId,
    string Reply,
    IReadOnlyList<AiSuggestArticleDto> SuggestedArticles,
    bool HandoffRequired,
    string? HandoffReason);
```

- [ ] **Step 6: Implement ChatMessageCommand**

```csharp
// src/CRM.Application/AI/Commands/ChatMessageCommand.cs
using CRM.Application.AI.DTOs;
using CRM.Domain.AI;
using CRM.Domain.Chat;
using MediatR;

namespace CRM.Application.AI.Commands;

public record ChatMessageCommand(
    Guid? SessionId,
    string Message,
    string Context,
    Guid RequestingUserId) : IRequest<AiChatMessageDto>;

public class ChatMessageCommandHandler : IRequestHandler<ChatMessageCommand, AiChatMessageDto>
{
    private static readonly string[] HandoffKeywords =
        ["human", "agent", "real person", "speak to someone"];

    private readonly IChatSessionRepository _sessions;
    private readonly IAzureOpenAiService _ai;

    public ChatMessageCommandHandler(IChatSessionRepository sessions, IAzureOpenAiService ai)
    {
        _sessions = sessions;
        _ai = ai;
    }

    public async Task<AiChatMessageDto> Handle(
        ChatMessageCommand cmd, CancellationToken ct)
    {
        ChatSession session;

        if (cmd.SessionId.HasValue)
        {
            session = await _sessions.FindByIdAsync(cmd.SessionId.Value, ct)
                ?? throw new KeyNotFoundException($"Session {cmd.SessionId} not found.");

            if (session.IsProcessing)
                throw new InvalidOperationException("SESSION_BUSY: Session is currently processing another message.");
        }
        else
        {
            session = ChatSession.Create(cmd.RequestingUserId, cmd.Context);
            await _sessions.AddAsync(session, ct);
        }

        session.SetProcessing(true);
        await _sessions.SaveChangesAsync(ct);

        try
        {
            session.AddMessage("Customer", cmd.Message);

            var contextMessages = session.Messages
                .TakeLast(20)
                .Select(m => new TicketMessageContext(m.SenderRole, m.Body, m.SentAt))
                .ToList();

            var aiResult = await _ai.SummarizeAsync(contextMessages, ct);
            session.AddMessage("AI", aiResult.Text);

            bool handoffRequired = false;
            string? handoffReason = null;

            if (cmd.Context != "agent")
            {
                var lowerMsg = cmd.Message.ToLowerInvariant();
                if (HandoffKeywords.Any(k => lowerMsg.Contains(k)))
                {
                    handoffRequired = true;
                    handoffReason = "customer requested human agent";
                }
                else if (session.FailedAttemptCount >= 3)
                {
                    handoffRequired = true;
                    handoffReason = "3 failed understanding attempts";
                }
                else if ((DateTime.UtcNow - session.CreatedAt).TotalMinutes > 10)
                {
                    handoffRequired = true;
                    handoffReason = "session exceeded 10 minutes unresolved";
                }

                if (handoffRequired) session.RequestHandoff();
            }

            await _sessions.SaveChangesAsync(ct);

            return new AiChatMessageDto(
                session.Id, aiResult.Text,
                Array.Empty<AiSuggestArticleDto>(),
                handoffRequired, handoffReason);
        }
        finally
        {
            session.SetProcessing(false);
            await _sessions.SaveChangesAsync(ct);
        }
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChatMessageCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Add ChatMessage action to AiController**

Open `src/CRM.API/Controllers/AiController.cs` and add:

```csharp
[HttpPost("chat/message")]
public async Task<IActionResult> ChatMessage(
    [FromBody] ChatMessageRequest req, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new ChatMessageCommand(req.SessionId, req.Message, req.Context, CurrentUserId), ct);
        return Ok(new { data = result });
    }
    catch (InvalidOperationException ex) when (ex.Message.StartsWith("SESSION_BUSY"))
        { return Conflict(new { error = "SESSION_BUSY" }); }
    catch (AiProviderException ex)
        { return StatusCode(503, new { error = ex.Message }); }
}
```

Add the request record:

```csharp
public record ChatMessageRequest(Guid? SessionId, string Message, string Context);
```

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Domain/Chat/ \
        src/CRM.Application/AI/DTOs/AiChatMessageDto.cs \
        src/CRM.Application/AI/Commands/ChatMessageCommand.cs \
        src/CRM.API/Controllers/AiController.cs \
        tests/CRM.Application.Tests/AI/ChatMessageCommandHandlerTests.cs
git commit -m "feat(ai): add POST /api/ai/chat/message — portal chatbot with handoff detection and session management"
```
