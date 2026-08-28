# Auto-Trigger CSAT Survey on Ticket Close — Implementation Plan

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

**Story:** US-BE-092  
**Goal:** On `TicketClosed` event: enqueue `SendCsatSurveyJob` (Hangfire). Job creates `CsatSurvey` with Status=Sent, snapshots `AgentId` and `DepartmentId` at close time. Unique constraint: one survey per ticket (skip if exists). Sends `SurveyAvailable` notification to customer (in-app). Sends survey email if `EmailVerified = true`.

**Architecture:** `TicketClosedEventHandler (MediatR)` → `IBackgroundJobClient.Enqueue<SendCsatSurveyJob>()`. Job: checks `ICsatSurveyRepository.ExistsForTicketAsync()`, creates survey, calls `CreateNotificationCommand`, optionally sends email.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/CSAT/Events/TicketClosedCsatEventHandler.cs` |
| Create | `src/CRM.Infrastructure/Jobs/SendCsatSurveyJob.cs` |
| Test   | `tests/CRM.Application.Tests/CSAT/TicketClosedCsatEventHandlerTests.cs` |
| Test   | `tests/CRM.Application.Tests/CSAT/SendCsatSurveyJobTests.cs` |

---

## Task 1: CSAT Auto-Trigger

> Note: `TicketClosedEvent` is from US-BE-081. `CsatSurvey` and `ICsatSurveyRepository` are from US-BE-082. `CreateNotificationCommand` is from US-BE-053. `ICustomerRepository` is from US-BE-009. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/CSAT/TicketClosedCsatEventHandlerTests.cs
using CRM.Application.CSAT.Events;
using CRM.Domain.Tickets.Events;
using Hangfire;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

public class TicketClosedCsatEventHandlerTests
{
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly TicketClosedCsatEventHandler _handler;

    public TicketClosedCsatEventHandlerTests()
    {
        _handler = new TicketClosedCsatEventHandler(_jobs.Object);
    }

    [Fact]
    public async Task Handle_TicketClosedEvent_EnqueuesSendCsatSurveyJob()
    {
        var evt = new TicketClosedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await _handler.Handle(evt, default);

        _jobs.Verify(j => j.Create(
            It.Is<Job>(job => job.Type == typeof(SendCsatSurveyJob)),
            It.IsAny<IState>()), Times.Once);
    }
}
```

```csharp
// tests/CRM.Application.Tests/CSAT/SendCsatSurveyJobTests.cs
using CRM.Application.Notifications.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Surveys;
using CRM.Domain.Tickets;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

public class SendCsatSurveyJobTests
{
    private readonly Mock<ICsatSurveyRepository> _surveys = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly SendCsatSurveyJob _job;

    public SendCsatSurveyJobTests()
    {
        _job = new SendCsatSurveyJob(
            _surveys.Object, _tickets.Object, _customers.Object, _mediator.Object);
    }

    [Fact]
    public async Task Execute_NoExistingSurvey_CreatesSurveyAndNotification()
    {
        var ticketId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var ticket = Ticket.Create("Issue", customerId, deptId, "Email");
        var customer = Customer.Create("Alice", "alice@example.com", null, null);

        _tickets.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _customers.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _surveys.Setup(r => r.ExistsForTicketAsync(ticketId, default)).ReturnsAsync(false);

        await _job.ExecuteAsync(ticketId, agentId, deptId);

        _surveys.Verify(r => r.AddAsync(It.IsAny<CsatSurvey>(), default), Times.Once);
        _mediator.Verify(m => m.Send(
            It.Is<CreateNotificationCommand>(c => c.Type == "SurveyAvailable"),
            default), Times.Once);
    }

    [Fact]
    public async Task Execute_ExistingSurvey_SkipsCreation()
    {
        var ticketId = Guid.NewGuid();
        _tickets.Setup(r => r.FindByIdAsync(ticketId, default))
                .ReturnsAsync(Ticket.Create("Issue", Guid.NewGuid(), Guid.NewGuid(), "Email"));
        _surveys.Setup(r => r.ExistsForTicketAsync(ticketId, default)).ReturnsAsync(true);

        await _job.ExecuteAsync(ticketId, Guid.NewGuid(), Guid.NewGuid());

        _surveys.Verify(r => r.AddAsync(It.IsAny<CsatSurvey>(), default), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketClosedCsatEventHandlerTests|SendCsatSurveyJobTests" -v n
```

Expected: FAIL — `TicketClosedCsatEventHandler` does not exist yet.

- [ ] **Step 3: Implement TicketClosedCsatEventHandler**

```csharp
// src/CRM.Application/CSAT/Events/TicketClosedCsatEventHandler.cs
using CRM.Domain.Tickets.Events;
using Hangfire;
using MediatR;

namespace CRM.Application.CSAT.Events;

public class TicketClosedCsatEventHandler : INotificationHandler<TicketClosedEvent>
{
    private readonly IBackgroundJobClient _jobs;
    public TicketClosedCsatEventHandler(IBackgroundJobClient jobs) => _jobs = jobs;

    public Task Handle(TicketClosedEvent notification, CancellationToken ct)
    {
        _jobs.Enqueue<SendCsatSurveyJob>(
            j => j.ExecuteAsync(
                notification.TicketId,
                notification.AgentId,
                notification.DepartmentId));
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 4: Implement SendCsatSurveyJob**

```csharp
// src/CRM.Infrastructure/Jobs/SendCsatSurveyJob.cs
using CRM.Application.Notifications.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Surveys;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Infrastructure.Jobs;

public class SendCsatSurveyJob
{
    private readonly ICsatSurveyRepository _surveys;
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;
    private readonly IMediator _mediator;

    public SendCsatSurveyJob(
        ICsatSurveyRepository surveys,
        ITicketRepository tickets,
        ICustomerRepository customers,
        IMediator mediator)
    {
        _surveys = surveys;
        _tickets = tickets;
        _customers = customers;
        _mediator = mediator;
    }

    public async Task ExecuteAsync(Guid ticketId, Guid agentId, Guid departmentId)
    {
        var ticket = await _tickets.FindByIdAsync(ticketId)
            ?? throw new Exception($"Ticket {ticketId} not found.");

        if (await _surveys.ExistsForTicketAsync(ticketId)) return;

        var customer = await _customers.FindByIdAsync(ticket.CustomerId)
            ?? throw new Exception($"Customer {ticket.CustomerId} not found.");

        var survey = CsatSurvey.Create(
            ticketId, customer.Id, agentId, departmentId,
            ticket.TicketNumber, ticket.Subject);

        await _surveys.AddAsync(survey);
        await _surveys.SaveChangesAsync();

        await _mediator.Send(new CreateNotificationCommand(
            UserId: customer.Id,
            Type: "SurveyAvailable",
            Title: "Rate your support experience",
            Body: $"Your ticket #{ticket.TicketNumber} was closed. Please rate your experience.",
            EntityType: "CsatSurvey",
            EntityId: survey.Id));
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "TicketClosedCsatEventHandlerTests|SendCsatSurveyJobTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/CSAT/Events/TicketClosedCsatEventHandler.cs \
        src/CRM.Infrastructure/Jobs/SendCsatSurveyJob.cs \
        tests/CRM.Application.Tests/CSAT/TicketClosedCsatEventHandlerTests.cs \
        tests/CRM.Application.Tests/CSAT/SendCsatSurveyJobTests.cs
git commit -m "feat(csat): auto-trigger CSAT survey on TicketClosed event via Hangfire SendCsatSurveyJob"
```
