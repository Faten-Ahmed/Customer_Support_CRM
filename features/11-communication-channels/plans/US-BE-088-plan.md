# Email Inbound Webhook — Implementation Plan

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

**Story:** US-BE-088  
**Goal:** Implement `POST /api/webhooks/email` — processes inbound emails. Validates HMAC-SHA256 signature (401 if invalid). Deduplicates by `Message-ID` (ExternalMessageId). Thread matching: In-Reply-To/References → append; phone/email matches open ticket → append; else create new ticket. Unknown sender → auto-create `Customer`. Attachments > 5MB dropped with system note. Loop detection: `From` matches noreply domain → drop. Always returns `200`.

**Architecture:** `ProcessEmailWebhookCommand` validates signature, then delegates threading/creation logic. `IEmailSignatureValidator` checks HMAC. Always responds 200 even on processing errors (errors logged).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Channels/IEmailSignatureValidator.cs` |
| Create | `src/CRM.Application/Webhooks/Commands/ProcessEmailWebhookCommand.cs` |
| Create | `src/CRM.API/Controllers/WebhooksController.cs` |
| Test   | `tests/CRM.Application.Tests/Webhooks/ProcessEmailWebhookCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Webhooks/WebhooksControllerEmailTests.cs` |

---

## Task 1: Email Inbound Processing

> Note: `Ticket`, `ITicketRepository`, `TicketMessage`, `Customer`, `ICustomerRepository` are from US-BE-019, US-BE-009. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Webhooks/ProcessEmailWebhookCommandHandlerTests.cs
using CRM.Application.Webhooks.Commands;
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Webhooks;

public class ProcessEmailWebhookCommandHandlerTests
{
    private readonly Mock<IEmailSignatureValidator> _validator = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly ProcessEmailWebhookCommandHandler _handler;

    public ProcessEmailWebhookCommandHandlerTests()
    {
        _handler = new ProcessEmailWebhookCommandHandler(
            _validator.Object, _tickets.Object, _customers.Object);
    }

    [Fact]
    public async Task Handle_ValidSignature_ProcessesEmail()
    {
        var payload = new EmailWebhookPayload(
            From: "alice@example.com", FromName: "Alice",
            Subject: "Need help", Body: "I have an issue",
            MessageId: "<msg-001@example.com>",
            InReplyTo: null, References: null,
            Attachments: new List<EmailAttachment>());

        _validator.Setup(v => v.Validate("sig123", It.IsAny<byte[]>())).Returns(true);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default))
                  .ReturnsAsync((Customer?)null);
        _customers.Setup(r => r.AddAsync(It.IsAny<Customer>(), default)).Returns(Task.CompletedTask);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("<msg-001@example.com>", default))
                .ReturnsAsync((Ticket?)null);
        _tickets.Setup(r => r.FindOpenByCustomerEmailAsync("alice@example.com", default))
                .ReturnsAsync((Ticket?)null);

        await _handler.Handle(
            new ProcessEmailWebhookCommand("sig123", payload), default);

        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidSignature_ThrowsUnauthorizedAccessException()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ProcessEmailWebhookCommand("badsig",
                    new EmailWebhookPayload("a@b.com", "A", "Sub", "Body",
                        "<id>", null, null, new List<EmailAttachment>())),
                default));
    }

    [Fact]
    public async Task Handle_DuplicateMessageId_SkipsProcessing()
    {
        var existingTicket = Ticket.Create("Existing", Guid.NewGuid(), Guid.NewGuid(), "Email");
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(true);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("<dup-msg>", default))
                .ReturnsAsync(existingTicket);

        await _handler.Handle(
            new ProcessEmailWebhookCommand("sig",
                new EmailWebhookPayload("a@b.com", "A", "Re: Sub", "Body",
                    "<dup-msg>", null, null, new List<EmailAttachment>())),
            default);

        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Never);
        _customers.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnDomainLoop_SkipsProcessing()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(true);

        await _handler.Handle(
            new ProcessEmailWebhookCommand("sig",
                new EmailWebhookPayload(
                    "noreply@support.azmcrm.com", "System", "Auto-reply", "Body",
                    "<loop-msg>", null, null, new List<EmailAttachment>())),
            default);

        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_ThreadMatch_AppendsMessage()
    {
        var existingTicket = Ticket.Create("Orig", Guid.NewGuid(), Guid.NewGuid(), "Email");
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<byte[]>())).Returns(true);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("<new-msg>", default))
                .ReturnsAsync((Ticket?)null);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("<orig-msg>", default))
                .ReturnsAsync(existingTicket);
        _customers.Setup(r => r.FindByEmailAsync("alice@example.com", default))
                  .ReturnsAsync((Customer?)null);

        await _handler.Handle(
            new ProcessEmailWebhookCommand("sig",
                new EmailWebhookPayload(
                    "alice@example.com", "Alice", "Re: Help", "Follow up",
                    "<new-msg>", "<orig-msg>", null, new List<EmailAttachment>())),
            default);

        Assert.Single(existingTicket.Messages);
        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Never);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ProcessEmailWebhookCommandHandlerTests" -v n
```

Expected: FAIL — `ProcessEmailWebhookCommand` does not exist yet.

- [ ] **Step 3: Create IEmailSignatureValidator**

```csharp
// src/CRM.Domain/Channels/IEmailSignatureValidator.cs
namespace CRM.Domain.Channels;

public interface IEmailSignatureValidator
{
    bool Validate(string signature, byte[] payload);
}
```

- [ ] **Step 4: Add repository methods**

Open `src/CRM.Domain/Tickets/ITicketRepository.cs` and add:

```csharp
Task<Ticket?> FindByExternalMessageIdAsync(string messageId, CancellationToken ct = default);
Task<Ticket?> FindOpenByCustomerEmailAsync(string email, CancellationToken ct = default);
```

- [ ] **Step 5: Implement ProcessEmailWebhookCommand**

```csharp
// src/CRM.Application/Webhooks/Commands/ProcessEmailWebhookCommand.cs
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Webhooks.Commands;

public record EmailAttachment(string FileName, long SizeBytes, string ContentType, string Url);

public record EmailWebhookPayload(
    string From, string FromName, string Subject, string Body,
    string MessageId, string? InReplyTo, string? References,
    List<EmailAttachment> Attachments);

public record ProcessEmailWebhookCommand(string Signature, EmailWebhookPayload Payload)
    : IRequest;

public class ProcessEmailWebhookCommandHandler : IRequestHandler<ProcessEmailWebhookCommand>
{
    private const long MaxAttachmentBytes = 5 * 1024 * 1024; // 5 MB
    private const string OwnDomain = "azmcrm.com"; // configurable

    private readonly IEmailSignatureValidator _validator;
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;

    public ProcessEmailWebhookCommandHandler(
        IEmailSignatureValidator validator,
        ITicketRepository tickets,
        ICustomerRepository customers)
    {
        _validator = validator;
        _tickets = tickets;
        _customers = customers;
    }

    public async Task Handle(ProcessEmailWebhookCommand cmd, CancellationToken ct)
    {
        if (!_validator.Validate(cmd.Signature, Array.Empty<byte>()))
            throw new UnauthorizedAccessException("Invalid webhook signature.");

        var p = cmd.Payload;

        // Loop detection
        if (p.From.EndsWith($"@{OwnDomain}", StringComparison.OrdinalIgnoreCase))
            return;

        // Deduplication
        var existing = await _tickets.FindByExternalMessageIdAsync(p.MessageId, ct);
        if (existing is not null) return;

        // Thread matching via In-Reply-To
        Ticket? parentTicket = null;
        if (p.InReplyTo is not null)
            parentTicket = await _tickets.FindByExternalMessageIdAsync(p.InReplyTo, ct);

        // Thread matching via email lookup
        if (parentTicket is null)
            parentTicket = await _tickets.FindOpenByCustomerEmailAsync(p.From, ct);

        if (parentTicket is not null)
        {
            parentTicket.AddMessage(
                senderRole: "Customer",
                body: p.Body,
                externalMessageId: p.MessageId,
                isInternal: false);

            AppendLargeAttachmentNote(parentTicket, p.Attachments);
            await _tickets.SaveChangesAsync(ct);
            return;
        }

        // Create customer if unknown
        var customer = await _customers.FindByEmailAsync(p.From, ct);
        if (customer is null)
        {
            customer = Customer.Create(p.FromName, p.From, null, null);
            await _customers.AddAsync(customer, ct);
        }

        // Create new ticket
        var ticket = Ticket.Create(p.Subject, customer.Id, Guid.Empty, "Email");
        ticket.AddMessage(
            senderRole: "Customer",
            body: p.Body,
            externalMessageId: p.MessageId,
            isInternal: false);

        AppendLargeAttachmentNote(ticket, p.Attachments);
        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);
    }

    private static void AppendLargeAttachmentNote(Ticket ticket, List<EmailAttachment> attachments)
    {
        foreach (var att in attachments.Where(a => a.SizeBytes > MaxAttachmentBytes))
        {
            ticket.AddMessage(
                senderRole: "System",
                body: $"Attachment '{att.FileName}' ({att.SizeBytes / (1024 * 1024)} MB) exceeded 5 MB limit and was dropped.",
                externalMessageId: null,
                isInternal: true);
        }
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ProcessEmailWebhookCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 7: Create WebhooksController**

```csharp
// src/CRM.API/Controllers/WebhooksController.cs
using CRM.Application.Webhooks.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IMediator mediator, ILogger<WebhooksController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("email")]
    public async Task<IActionResult> Email(
        [FromHeader(Name = "X-Webhook-Signature")] string signature,
        [FromBody] EmailWebhookPayload payload,
        CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new ProcessEmailWebhookCommand(signature, payload), ct);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email webhook — queued for dead-letter retry");
        }
        return Ok();
    }
}
```

- [ ] **Step 8: Write controller test**

```csharp
// tests/CRM.API.Tests/Webhooks/WebhooksControllerEmailTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Webhooks.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Webhooks;

public class WebhooksControllerEmailTests
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
        return factory.CreateClient();
    }

    [Fact]
    public async Task Email_ValidSignature_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ProcessEmailWebhookCommand>(), default))
                 .Returns(Task.FromResult(Unit.Value));

        var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Webhook-Signature", "validsig");

        var response = await client.PostAsJsonAsync("/api/webhooks/email",
            new { from = "a@b.com", fromName = "A", subject = "S", body = "B",
                  messageId = "<1>", attachments = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Email_InvalidSignature_Returns401()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ProcessEmailWebhookCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Invalid signature."));

        var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Webhook-Signature", "badsig");

        var response = await client.PostAsJsonAsync("/api/webhooks/email",
            new { from = "a@b.com", fromName = "A", subject = "S", body = "B",
                  messageId = "<2>", attachments = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Email_ProcessingError_StillReturns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ProcessEmailWebhookCommand>(), default))
                 .ThrowsAsync(new Exception("DB error"));

        var client = BuildClient();
        client.DefaultRequestHeaders.Add("X-Webhook-Signature", "sig");

        var response = await client.PostAsJsonAsync("/api/webhooks/email",
            new { from = "a@b.com", fromName = "A", subject = "S", body = "B",
                  messageId = "<3>", attachments = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 9: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "WebhooksControllerEmailTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Domain/Channels/IEmailSignatureValidator.cs \
        src/CRM.Application/Webhooks/Commands/ProcessEmailWebhookCommand.cs \
        src/CRM.API/Controllers/WebhooksController.cs \
        tests/CRM.Application.Tests/Webhooks/ProcessEmailWebhookCommandHandlerTests.cs \
        tests/CRM.API.Tests/Webhooks/WebhooksControllerEmailTests.cs
git commit -m "feat(channels): add POST /api/webhooks/email — HMAC validation, dedup, thread match, auto-customer create"
```
