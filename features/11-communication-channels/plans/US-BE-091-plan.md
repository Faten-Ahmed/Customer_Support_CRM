# Live Chat SignalR Hub — Implementation Plan

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

**Story:** US-BE-091  
**Goal:** Implement `ChatHub` at `/hubs/chat` — SignalR hub for live chat. `JoinSession`, `RequestHandoff`, `AgentAcceptHandoff`, `SendMessage` (dual-write ChatSessionMessage + TicketMessage), `CloseSession`, `AgentTyping`/`CustomerTyping` (fire-and-forget), `SubscribeToDepartment`. Session group: `chat-{sessionId}`. Dept group: `dept-chat-{deptId}`.

**Architecture:** `ChatHub : Hub` with JWT auth. Hub methods dispatch MediatR commands for state changes. `IChatSessionRepository` from US-BE-087. Handoff creates `Ticket` from transcript.

**Tech Stack:** .NET 10, ASP.NET Core, SignalR, MediatR, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Chat/Commands/RequestHandoffCommand.cs` |
| Create | `src/CRM.Application/Chat/Commands/AcceptHandoffCommand.cs` |
| Create | `src/CRM.Application/Chat/Commands/SendChatMessageCommand.cs` |
| Create | `src/CRM.Application/Chat/Commands/CloseSessionCommand.cs` |
| Create | `src/CRM.Infrastructure/Hubs/ChatHub.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Chat/ChatCommandHandlerTests.cs` |

---

## Task 1: Chat Hub Commands

> Note: `ChatSession`, `IChatSessionRepository` are from US-BE-087. `ITicketRepository`, `Ticket` are from US-BE-019. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Chat/ChatCommandHandlerTests.cs
using CRM.Application.Chat.Commands;
using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Chat;

public class ChatCommandHandlerTests
{
    private readonly Mock<IChatSessionRepository> _sessions = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IPublisher> _publisher = new();

    private RequestHandoffCommandHandler NewRequestHandoffHandler()
        => new(_sessions.Object, _tickets.Object, _publisher.Object);

    private AcceptHandoffCommandHandler NewAcceptHandoffHandler()
        => new(_sessions.Object, _publisher.Object);

    private SendChatMessageCommandHandler NewSendMessageHandler()
        => new(_sessions.Object, _tickets.Object);

    private CloseSessionCommandHandler NewCloseSessionHandler()
        => new(_sessions.Object, _tickets.Object, _publisher.Object);

    [Fact]
    public async Task RequestHandoff_CreatesTicketFromTranscript()
    {
        var customerId = Guid.NewGuid();
        var session = ChatSession.Create(customerId, "portal");
        session.AddMessage("Customer", "I need help");
        session.AddMessage("AI", "How can I assist?");
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);

        await NewRequestHandoffHandler().Handle(
            new RequestHandoffCommand(session.Id, customerId, Guid.NewGuid()), default);

        _tickets.Verify(r => r.AddAsync(It.Is<Ticket>(t => t.Channel == "LiveChat"), default),
            Times.Once);
        Assert.Equal("HandoffPending", session.Status);
    }

    [Fact]
    public async Task AcceptHandoff_SetsAgentAndConnectedStatus()
    {
        var agentId = Guid.NewGuid();
        var session = ChatSession.Create(Guid.NewGuid(), "portal");
        session.RequestHandoff();
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);

        await NewAcceptHandoffHandler().Handle(
            new AcceptHandoffCommand(session.Id, agentId), default);

        Assert.Equal(agentId, session.AgentId);
        Assert.Equal("AgentConnected", session.Status);
    }

    [Fact]
    public async Task SendMessage_DualWritesChatAndTicketMessage()
    {
        var senderId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var session = ChatSession.Create(senderId, "portal");
        session.SetLinkedTicketId(ticketId);
        var ticket = Ticket.Create("Chat", Guid.NewGuid(), Guid.NewGuid(), "LiveChat");
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);
        _tickets.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await NewSendMessageHandler().Handle(
            new SendChatMessageCommand(session.Id, senderId, "Customer", "Hello agent!"),
            default);

        Assert.Single(session.Messages);
        Assert.Single(ticket.Messages);
    }

    [Fact]
    public async Task CloseSession_WithResolvedResolution_ResolvesLinkedTicket()
    {
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var session = ChatSession.Create(userId, "portal");
        session.SetLinkedTicketId(ticketId);
        var ticket = Ticket.Create("Chat", Guid.NewGuid(), Guid.NewGuid(), "LiveChat");
        _sessions.Setup(r => r.FindByIdAsync(session.Id, default)).ReturnsAsync(session);
        _tickets.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await NewCloseSessionHandler().Handle(
            new CloseSessionCommand(session.Id, userId, "Resolved"), default);

        Assert.Equal("Resolved", ticket.Status);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChatCommandHandlerTests" -v n
```

Expected: FAIL — `RequestHandoffCommand` does not exist yet.

- [ ] **Step 3: Extend ChatSession entity**

Open `src/CRM.Domain/Chat/ChatSession.cs` and add:

```csharp
public Guid? AgentId { get; private set; }
public Guid? LinkedTicketId { get; private set; }

public void AcceptHandoff(Guid agentId)
{
    AgentId = agentId;
    Status = "AgentConnected";
}

public void SetLinkedTicketId(Guid ticketId) => LinkedTicketId = ticketId;

public void Close() => Status = "Closed";
```

- [ ] **Step 4: Implement RequestHandoffCommand**

```csharp
// src/CRM.Application/Chat/Commands/RequestHandoffCommand.cs
using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Chat.Commands;

public record RequestHandoffCommand(Guid SessionId, Guid CustomerId, Guid DepartmentId) : IRequest;

public class RequestHandoffCommandHandler : IRequestHandler<RequestHandoffCommand>
{
    private readonly IChatSessionRepository _sessions;
    private readonly ITicketRepository _tickets;
    private readonly IPublisher _publisher;

    public RequestHandoffCommandHandler(
        IChatSessionRepository sessions, ITicketRepository tickets, IPublisher publisher)
    {
        _sessions = sessions;
        _tickets = tickets;
        _publisher = publisher;
    }

    public async Task Handle(RequestHandoffCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.FindByIdAsync(cmd.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {cmd.SessionId} not found.");

        var transcript = string.Join("\n", session.Messages.Select(
            m => $"[{m.SentAt:HH:mm}] {m.SenderRole}: {m.Body}"));

        var ticket = Ticket.Create(
            $"Live Chat — {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            cmd.CustomerId, cmd.DepartmentId, "LiveChat");
        ticket.AddMessage("Customer", transcript, null, false);

        session.RequestHandoff();
        session.SetLinkedTicketId(ticket.Id);

        await _tickets.AddAsync(ticket, ct);
        await _sessions.SaveChangesAsync(ct);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Implement AcceptHandoffCommand**

```csharp
// src/CRM.Application/Chat/Commands/AcceptHandoffCommand.cs
using CRM.Domain.Chat;
using MediatR;

namespace CRM.Application.Chat.Commands;

public record AcceptHandoffCommand(Guid SessionId, Guid AgentId) : IRequest;

public class AcceptHandoffCommandHandler : IRequestHandler<AcceptHandoffCommand>
{
    private readonly IChatSessionRepository _sessions;
    private readonly IPublisher _publisher;

    public AcceptHandoffCommandHandler(IChatSessionRepository sessions, IPublisher publisher)
    {
        _sessions = sessions;
        _publisher = publisher;
    }

    public async Task Handle(AcceptHandoffCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.FindByIdAsync(cmd.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {cmd.SessionId} not found.");

        session.AcceptHandoff(cmd.AgentId);
        await _sessions.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 6: Implement SendChatMessageCommand**

```csharp
// src/CRM.Application/Chat/Commands/SendChatMessageCommand.cs
using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Chat.Commands;

public record SendChatMessageCommand(
    Guid SessionId, Guid SenderId, string SenderRole, string Body) : IRequest;

public class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand>
{
    private readonly IChatSessionRepository _sessions;
    private readonly ITicketRepository _tickets;

    public SendChatMessageCommandHandler(
        IChatSessionRepository sessions, ITicketRepository tickets)
    {
        _sessions = sessions;
        _tickets = tickets;
    }

    public async Task Handle(SendChatMessageCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.FindByIdAsync(cmd.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {cmd.SessionId} not found.");

        session.AddMessage(cmd.SenderRole, cmd.Body);

        if (session.LinkedTicketId.HasValue)
        {
            var ticket = await _tickets.FindByIdAsync(session.LinkedTicketId.Value, ct);
            ticket?.AddMessage(cmd.SenderRole, cmd.Body, null, false);
            await _tickets.SaveChangesAsync(ct);
        }

        await _sessions.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Implement CloseSessionCommand**

```csharp
// src/CRM.Application/Chat/Commands/CloseSessionCommand.cs
using CRM.Domain.Chat;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Chat.Commands;

public record CloseSessionCommand(Guid SessionId, Guid UserId, string Resolution) : IRequest;

public class CloseSessionCommandHandler : IRequestHandler<CloseSessionCommand>
{
    private readonly IChatSessionRepository _sessions;
    private readonly ITicketRepository _tickets;
    private readonly IPublisher _publisher;

    public CloseSessionCommandHandler(
        IChatSessionRepository sessions, ITicketRepository tickets, IPublisher publisher)
    {
        _sessions = sessions;
        _tickets = tickets;
        _publisher = publisher;
    }

    public async Task Handle(CloseSessionCommand cmd, CancellationToken ct)
    {
        var session = await _sessions.FindByIdAsync(cmd.SessionId, ct)
            ?? throw new KeyNotFoundException($"Session {cmd.SessionId} not found.");

        session.Close();

        if (session.LinkedTicketId.HasValue)
        {
            var ticket = await _tickets.FindByIdAsync(session.LinkedTicketId.Value, ct);
            if (ticket is not null)
            {
                if (cmd.Resolution == "Resolved")
                    ticket.Status = "Resolved";
                else if (cmd.Resolution == "Escalated")
                    ticket.Status = "Escalated";
                await _tickets.SaveChangesAsync(ct);
            }
        }

        await _sessions.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ChatCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 9: Create ChatHub**

```csharp
// src/CRM.Infrastructure/Hubs/ChatHub.cs
using CRM.Application.Chat.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRM.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    public ChatHub(IMediator mediator) => _mediator = mediator;

    public async Task JoinSession(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"chat-{sessionId}");
    }

    public async Task SubscribeToDepartment(Guid departmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"dept-chat-{departmentId}");
    }

    public async Task RequestHandoff(Guid sessionId, Guid departmentId)
    {
        var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _mediator.Send(new RequestHandoffCommand(sessionId, userId, departmentId));
        await Clients.Group($"dept-chat-{departmentId}")
            .SendAsync("HandoffRequested", sessionId);
    }

    public async Task AgentAcceptHandoff(Guid sessionId)
    {
        var agentId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _mediator.Send(new AcceptHandoffCommand(sessionId, agentId));
        await Clients.Group($"chat-{sessionId}")
            .SendAsync("HandoffAccepted", agentId);
    }

    public async Task SendMessage(Guid sessionId, string body)
    {
        var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var role = Context.User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        await _mediator.Send(new SendChatMessageCommand(sessionId, userId, role, body));
        await Clients.Group($"chat-{sessionId}").SendAsync("ReceiveMessage", role, body);
    }

    public async Task CloseSession(Guid sessionId, string resolution)
    {
        var userId = Guid.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _mediator.Send(new CloseSessionCommand(sessionId, userId, resolution));
        await Clients.Group($"chat-{sessionId}").SendAsync("SessionClosed", resolution);
    }

    public async Task AgentTyping(Guid sessionId)
        => await Clients.OthersInGroup($"chat-{sessionId}").SendAsync("AgentTyping");

    public async Task CustomerTyping(Guid sessionId)
        => await Clients.OthersInGroup($"chat-{sessionId}").SendAsync("CustomerTyping");
}
```

- [ ] **Step 10: Register ChatHub in Program.cs**

Open `src/CRM.API/Program.cs` and add in the hub mapping section:

```csharp
app.MapHub<ChatHub>("/hubs/chat");
```

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Application/Chat/Commands/ \
        src/CRM.Infrastructure/Hubs/ChatHub.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Chat/ChatCommandHandlerTests.cs
git commit -m "feat(channels): add ChatHub — live chat with handoff, dual-write messages, session/ticket lifecycle"
```
