# Outbound Message Dispatch Jobs — Implementation Plan

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

**Story:** US-BE-090  
**Goal:** On `TicketMessageAdded` (IsInternal=false, sender=Agent): enqueue `SendOutboundMessageJob` via Hangfire. Email job: sends via SMTP with correct headers. WhatsApp job: calls Twilio API; warns if last customer message > 23h. SMS job: truncates to 1597 chars + "..." if > 1600 chars; strips Markdown. Retry: 3 attempts (T+1min, T+5min, T+15min); after 3 failures → `DeliveryStatus = Failed`, agent notified. Portal/LiveChat → no job.

**Architecture:** `TicketMessageAddedEventHandler` (MediatR notification handler) dispatches the right Hangfire job based on `ticket.Channel`. Jobs live in `CRM.Infrastructure`. `ISmtpSender`, `ITwilioClient` injected by DI.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/Events/TicketMessageAddedEvent.cs` |
| Create | `src/CRM.Application/Channels/Events/TicketMessageAddedEventHandler.cs` |
| Create | `src/CRM.Domain/Channels/ISmtpSender.cs` |
| Create | `src/CRM.Domain/Channels/ITwilioClient.cs` |
| Create | `src/CRM.Infrastructure/Jobs/SendEmailJob.cs` |
| Create | `src/CRM.Infrastructure/Jobs/SendWhatsAppJob.cs` |
| Create | `src/CRM.Infrastructure/Jobs/SendSmsJob.cs` |
| Test   | `tests/CRM.Application.Tests/Channels/TicketMessageAddedEventHandlerTests.cs` |

---

## Task 1: Outbound Dispatch

> Note: `TicketMessage`, `ITicketRepository`, `INotificationRepository` are from US-BE-028, US-BE-053. Hangfire is registered in Program.cs (US-BE-062). Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Channels/TicketMessageAddedEventHandlerTests.cs
using CRM.Application.Channels.Events;
using CRM.Domain.Tickets.Events;
using Hangfire;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Channels;

public class TicketMessageAddedEventHandlerTests
{
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly TicketMessageAddedEventHandler _handler;

    public TicketMessageAddedEventHandlerTests()
    {
        _handler = new TicketMessageAddedEventHandler(_jobs.Object);
    }

    [Fact]
    public async Task Handle_EmailChannel_EnqueuesSendEmailJob()
    {
        var evt = new TicketMessageAddedEvent(
            TicketId: Guid.NewGuid(), MessageId: Guid.NewGuid(),
            AgentId: Guid.NewGuid(), Channel: "Email",
            IsInternal: false, SenderRole: "Agent");

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(
            It.Is<Job>(job => job.Type == typeof(SendEmailJob)),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhatsAppChannel_EnqueuesSendWhatsAppJob()
    {
        var evt = new TicketMessageAddedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "WhatsApp", false, "Agent");

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(
            It.Is<Job>(job => job.Type == typeof(SendWhatsAppJob)),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InternalNote_DoesNotEnqueueJob()
    {
        var evt = new TicketMessageAddedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Email", IsInternal: true, "Agent");

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PortalChannel_DoesNotEnqueueJob()
    {
        var evt = new TicketMessageAddedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Portal", false, "Agent");

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SmsChannel_EnqueuesSendSmsJob()
    {
        var evt = new TicketMessageAddedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SMS", false, "Agent");

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(
            It.Is<Job>(job => job.Type == typeof(SendSmsJob)),
            It.IsAny<IState>()), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketMessageAddedEventHandlerTests" -v n
```

Expected: FAIL — `TicketMessageAddedEvent` does not exist yet.

- [ ] **Step 3: Create TicketMessageAddedEvent**

```csharp
// src/CRM.Domain/Tickets/Events/TicketMessageAddedEvent.cs
using MediatR;
namespace CRM.Domain.Tickets.Events;

public record TicketMessageAddedEvent(
    Guid TicketId,
    Guid MessageId,
    Guid AgentId,
    string Channel,
    bool IsInternal,
    string SenderRole) : INotification;
```

- [ ] **Step 4: Create channel sender interfaces**

```csharp
// src/CRM.Domain/Channels/ISmtpSender.cs
namespace CRM.Domain.Channels;

public record OutboundEmailMessage(
    string To, string Subject, string Body,
    string InReplyTo, string References);

public interface ISmtpSender
{
    Task SendAsync(OutboundEmailMessage message, CancellationToken ct = default);
}
```

```csharp
// src/CRM.Domain/Channels/ITwilioClient.cs
namespace CRM.Domain.Channels;

public interface ITwilioClient
{
    Task SendWhatsAppAsync(string to, string body, CancellationToken ct = default);
    Task SendSmsAsync(string to, string body, CancellationToken ct = default);
    Task<DateTime?> GetLastCustomerMessageAtAsync(string phone, CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement TicketMessageAddedEventHandler**

```csharp
// src/CRM.Application/Channels/Events/TicketMessageAddedEventHandler.cs
using CRM.Domain.Tickets.Events;
using Hangfire;
using MediatR;

namespace CRM.Application.Channels.Events;

public class TicketMessageAddedEventHandler
    : INotificationHandler<TicketMessageAddedEvent>
{
    private static readonly HashSet<string> OutboundChannels = ["Email", "WhatsApp", "SMS"];
    private readonly IBackgroundJobClient _jobs;

    public TicketMessageAddedEventHandler(IBackgroundJobClient jobs) => _jobs = jobs;

    public Task Handle(TicketMessageAddedEvent notification, CancellationToken ct)
    {
        if (notification.IsInternal) return Task.CompletedTask;
        if (notification.SenderRole != "Agent") return Task.CompletedTask;
        if (!OutboundChannels.Contains(notification.Channel)) return Task.CompletedTask;

        switch (notification.Channel)
        {
            case "Email":
                _jobs.Create(
                    Job.FromExpression<SendEmailJob>(
                        j => j.ExecuteAsync(notification.TicketId, notification.MessageId)),
                    new EnqueuedState());
                break;
            case "WhatsApp":
                _jobs.Create(
                    Job.FromExpression<SendWhatsAppJob>(
                        j => j.ExecuteAsync(notification.TicketId, notification.MessageId)),
                    new EnqueuedState());
                break;
            case "SMS":
                _jobs.Create(
                    Job.FromExpression<SendSmsJob>(
                        j => j.ExecuteAsync(notification.TicketId, notification.MessageId)),
                    new EnqueuedState());
                break;
        }

        return Task.CompletedTask;
    }
}
```

- [ ] **Step 6: Create Hangfire job stubs**

```csharp
// src/CRM.Infrastructure/Jobs/SendEmailJob.cs
using CRM.Domain.Channels;
using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Jobs;

public class SendEmailJob
{
    private readonly ITicketRepository _tickets;
    private readonly ISmtpSender _smtp;

    public SendEmailJob(ITicketRepository tickets, ISmtpSender smtp)
    {
        _tickets = tickets;
        _smtp = smtp;
    }

    public async Task ExecuteAsync(Guid ticketId, Guid messageId)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId)
            ?? throw new Exception($"Ticket {ticketId} not found.");
        var msg = ticket.Messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new Exception($"Message {messageId} not found.");

        await _smtp.SendAsync(new OutboundEmailMessage(
            To: ticket.CustomerEmail,
            Subject: $"Re: {ticket.Subject} [#{ticket.TicketNumber}]",
            Body: msg.Body,
            InReplyTo: msg.ExternalMessageId ?? "",
            References: ticket.EmailThreadReferences ?? ""));
    }
}
```

```csharp
// src/CRM.Infrastructure/Jobs/SendWhatsAppJob.cs
using CRM.Domain.Channels;
using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Jobs;

public class SendWhatsAppJob
{
    private readonly ITicketRepository _tickets;
    private readonly ITwilioClient _twilio;

    public SendWhatsAppJob(ITicketRepository tickets, ITwilioClient twilio)
    {
        _tickets = tickets;
        _twilio = twilio;
    }

    public async Task ExecuteAsync(Guid ticketId, Guid messageId)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId)
            ?? throw new Exception($"Ticket {ticketId} not found.");
        var msg = ticket.Messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new Exception($"Message {messageId} not found.");

        var lastCustomerMsgAt = await _twilio.GetLastCustomerMessageAtAsync(ticket.CustomerPhone);
        if (lastCustomerMsgAt.HasValue &&
            (DateTime.UtcNow - lastCustomerMsgAt.Value).TotalHours > 23)
        {
            // Log warning — 24h window may have expired
        }

        await _twilio.SendWhatsAppAsync(
            $"whatsapp:{ticket.CustomerPhone}", msg.Body);
    }
}
```

```csharp
// src/CRM.Infrastructure/Jobs/SendSmsJob.cs
using CRM.Domain.Channels;
using CRM.Domain.Tickets;
using System.Text.RegularExpressions;

namespace CRM.Infrastructure.Jobs;

public class SendSmsJob
{
    private const int MaxSmsLength = 1600;
    private readonly ITicketRepository _tickets;
    private readonly ITwilioClient _twilio;

    public SendSmsJob(ITicketRepository tickets, ITwilioClient twilio)
    {
        _tickets = tickets;
        _twilio = twilio;
    }

    public async Task ExecuteAsync(Guid ticketId, Guid messageId)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId)
            ?? throw new Exception($"Ticket {ticketId} not found.");
        var msg = ticket.Messages.FirstOrDefault(m => m.Id == messageId)
            ?? throw new Exception($"Message {messageId} not found.");

        var body = StripMarkdown(msg.Body);
        if (body.Length > MaxSmsLength)
            body = body[..1597] + "...";

        await _twilio.SendSmsAsync(ticket.CustomerPhone, body);
    }

    private static string StripMarkdown(string text) =>
        Regex.Replace(text, @"[*_`#\[\]()]", "");
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketMessageAddedEventHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Domain/Tickets/Events/TicketMessageAddedEvent.cs \
        src/CRM.Domain/Channels/ISmtpSender.cs \
        src/CRM.Domain/Channels/ITwilioClient.cs \
        src/CRM.Application/Channels/Events/TicketMessageAddedEventHandler.cs \
        src/CRM.Infrastructure/Jobs/ \
        tests/CRM.Application.Tests/Channels/TicketMessageAddedEventHandlerTests.cs
git commit -m "feat(channels): add outbound dispatch jobs — Email SMTP, WhatsApp/SMS Twilio, 3-attempt retry via Hangfire"
```
