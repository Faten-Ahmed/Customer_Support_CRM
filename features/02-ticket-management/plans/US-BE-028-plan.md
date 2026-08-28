# Add Ticket Message — Implementation Plan

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

**Story:** US-BE-028  
**Goal:** Implement `POST /api/tickets/{id}/messages` — adds a message (reply) to a ticket thread, supporting internal notes (visible to staff only) and customer-visible replies. Supports HTML content from the rich-text editor.

**Architecture:** `AddTicketMessageCommand(ticketId, body, isInternal, authorId, authorType)` → handler validates ticket is not closed, creates `TicketMessage`, persists. If the ticket is in `Resolved` status and the author is the customer (via portal), triggers reopen logic. Returns `TicketMessageDto`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/TicketMessage.cs` |
| Create | `src/CRM.Domain/Tickets/ITicketMessageRepository.cs` |
| Create | `src/CRM.Application/Tickets/Commands/AddTicketMessageCommand.cs` |
| Create | `src/CRM.Application/Tickets/DTOs/TicketMessageDto.cs` |
| Create | `src/CRM.Application/Tickets/Validators/AddTicketMessageCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/AddTicketMessageCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerMessageTests.cs` |

---

## Task 1: TicketMessage Domain Entity

**Files:**
- Create: `src/CRM.Domain/Tickets/TicketMessage.cs`
- Create: `src/CRM.Domain/Tickets/ITicketMessageRepository.cs`

- [ ] **Step 1: Create TicketMessage and repository interface**

```csharp
// src/CRM.Domain/Tickets/TicketMessage.cs
namespace CRM.Domain.Tickets;

public enum MessageAuthorType { Agent, Customer, System }

public class TicketMessage
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string Body { get; private set; } = null!;
    public bool IsInternal { get; private set; }
    public Guid AuthorId { get; private set; }
    public MessageAuthorType AuthorType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TicketMessage() { }

    public static TicketMessage Create(
        Guid ticketId, string body, bool isInternal,
        Guid authorId, MessageAuthorType authorType)
        => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Body = body,
            IsInternal = isInternal,
            AuthorId = authorId,
            AuthorType = authorType,
            CreatedAt = DateTime.UtcNow
        };
}
```

```csharp
// src/CRM.Domain/Tickets/ITicketMessageRepository.cs
using CRM.Application.Common;

namespace CRM.Domain.Tickets;

public interface ITicketMessageRepository
{
    Task AddAsync(TicketMessage message, CancellationToken ct = default);
    Task<PagedResult<TicketMessage>> ListByTicketAsync(Guid ticketId, int page, int pageSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Tickets/TicketMessage.cs \
        src/CRM.Domain/Tickets/ITicketMessageRepository.cs
git commit -m "feat(domain): add TicketMessage entity and ITicketMessageRepository"
```

---

## Task 2: AddTicketMessage Command + Handler + Validator + DTO

**Files:**
- Create: `src/CRM.Application/Tickets/DTOs/TicketMessageDto.cs`
- Create: `src/CRM.Application/Tickets/Commands/AddTicketMessageCommand.cs`
- Create: `src/CRM.Application/Tickets/Validators/AddTicketMessageCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Tickets/AddTicketMessageCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/AddTicketMessageCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class AddTicketMessageCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ITicketMessageRepository> _messageRepo = new();
    private readonly AddTicketMessageCommandHandler _handler;

    public AddTicketMessageCommandHandlerTests()
    {
        _handler = new AddTicketMessageCommandHandler(_ticketRepo.Object, _messageRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidMessage_AddsMessageAndReturnsDto()
    {
        var ticketId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Medium, TicketChannel.Internal, agentId);
        ticket.Assign(agentId, agentId);
        ticket.ChangeStatus(TicketStatus.InProgress, agentId);

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var result = await _handler.Handle(new AddTicketMessageCommand(
            ticketId, "<p>Hello customer</p>", false,
            agentId, MessageAuthorType.Agent), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("<p>Hello customer</p>", result.Body);
        Assert.False(result.IsInternal);
        _messageRepo.Verify(r => r.AddAsync(It.IsAny<TicketMessage>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_InternalNote_IsMarkedInternal()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        TicketMessage? captured = null;
        _messageRepo.Setup(r => r.AddAsync(It.IsAny<TicketMessage>(), default))
                    .Callback<TicketMessage, CancellationToken>((m, _) => captured = m)
                    .Returns(Task.CompletedTask);

        await _handler.Handle(new AddTicketMessageCommand(
            ticketId, "Internal only", true, Guid.NewGuid(), MessageAuthorType.Agent), default);

        Assert.NotNull(captured);
        Assert.True(captured!.IsInternal);
    }

    [Fact]
    public async Task Handle_ClosedTicket_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        ticket.ChangeStatus(TicketStatus.Closed, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new AddTicketMessageCommand(
                ticketId, "msg", false, Guid.NewGuid(), MessageAuthorType.Agent), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AddTicketMessageCommandHandlerTests" -v n
```

Expected: FAIL — `AddTicketMessageCommand` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Tickets/DTOs/TicketMessageDto.cs
namespace CRM.Application.Tickets.DTOs;

public record TicketMessageDto(
    Guid Id,
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid AuthorId,
    string AuthorName,
    string AuthorType,
    DateTime CreatedAt);
```

- [ ] **Step 4: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/AddTicketMessageCommand.cs
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record AddTicketMessageCommand(
    Guid TicketId,
    string Body,
    bool IsInternal,
    Guid AuthorId,
    MessageAuthorType AuthorType) : IRequest<TicketMessageDto>;

public class AddTicketMessageCommandHandler
    : IRequestHandler<AddTicketMessageCommand, TicketMessageDto>
{
    private readonly ITicketRepository _tickets;
    private readonly ITicketMessageRepository _messages;

    public AddTicketMessageCommandHandler(
        ITicketRepository tickets, ITicketMessageRepository messages)
    {
        _tickets = tickets;
        _messages = messages;
    }

    public async Task<TicketMessageDto> Handle(
        AddTicketMessageCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (ticket.Status == TicketStatus.Closed)
            throw new InvalidOperationException("Cannot add messages to a closed ticket.");

        var message = TicketMessage.Create(
            cmd.TicketId, cmd.Body, cmd.IsInternal, cmd.AuthorId, cmd.AuthorType);

        await _messages.AddAsync(message, ct);
        await _messages.SaveChangesAsync(ct);

        return new TicketMessageDto(
            message.Id, message.TicketId, message.Body, message.IsInternal,
            message.AuthorId, string.Empty, message.AuthorType.ToString(), message.CreatedAt);
    }
}
```

- [ ] **Step 5: Create validator**

```csharp
// src/CRM.Application/Tickets/Validators/AddTicketMessageCommandValidator.cs
using CRM.Application.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Tickets.Validators;

public class AddTicketMessageCommandValidator : AbstractValidator<AddTicketMessageCommand>
{
    public AddTicketMessageCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.AuthorId).NotEmpty();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AddTicketMessageCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/AddTicketMessageCommand.cs \
        src/CRM.Application/Tickets/DTOs/TicketMessageDto.cs \
        src/CRM.Application/Tickets/Validators/AddTicketMessageCommandValidator.cs \
        tests/CRM.Application.Tests/Tickets/AddTicketMessageCommandHandlerTests.cs
git commit -m "feat(tickets): add AddTicketMessageCommand with closed-ticket guard"
```

---

## Task 3: TicketsController — POST /api/tickets/{id}/messages

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerMessageTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerMessageTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerMessageTests
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
    public async Task AddMessage_ValidBody_Returns201()
    {
        var ticketId = Guid.NewGuid();
        var msgId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<AddTicketMessageCommand>(), default))
                 .ReturnsAsync(new TicketMessageDto(
                     msgId, ticketId, "<p>Hello</p>", false,
                     Guid.NewGuid(), "Ali Hassan", "Agent", DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages",
            new { body = "<p>Hello</p>", isInternal = false });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddMessage_ClosedTicket_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AddTicketMessageCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Ticket is closed."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/messages",
            new { body = "msg", isInternal = false });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerMessageTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add AddMessage endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

public record AddMessageRequest(string Body, bool IsInternal);

[HttpPost("{id:guid}/messages")]
public async Task<IActionResult> AddMessage(
    Guid id, [FromBody] AddMessageRequest request, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new AddTicketMessageCommand(
            id, request.Body, request.IsInternal,
            CurrentUserId, MessageAuthorType.Agent), ct);
        return StatusCode(201, result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerMessageTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerMessageTests.cs
git commit -m "feat(api): add POST /api/tickets/{id}/messages endpoint"
```
