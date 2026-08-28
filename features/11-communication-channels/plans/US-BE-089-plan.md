# WhatsApp & SMS Inbound Webhooks — Implementation Plan

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

**Story:** US-BE-089  
**Goal:** Implement `POST /api/webhooks/whatsapp` and `POST /api/webhooks/sms` — both validate Twilio signature (`X-Twilio-Signature`), 403 if invalid. WhatsApp: strips `whatsapp:` prefix, normalises phone to E.164. SMS: deduplicates by `MessageSid`. Media > 5 MB dropped with system note. Unknown sender auto-creates `Customer`. Both return TwiML `<Response/>` (empty body). Always returns 200 on processing errors.

**Architecture:** `ProcessWhatsAppWebhookCommand` and `ProcessSmsWebhookCommand` share `ITwilioSignatureValidator`. Thread matching by phone number to open tickets. Both added to `WebhooksController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Channels/ITwilioSignatureValidator.cs` |
| Create | `src/CRM.Application/Webhooks/Commands/ProcessWhatsAppWebhookCommand.cs` |
| Create | `src/CRM.Application/Webhooks/Commands/ProcessSmsWebhookCommand.cs` |
| Modify | `src/CRM.API/Controllers/WebhooksController.cs` |
| Test   | `tests/CRM.Application.Tests/Webhooks/ProcessWhatsAppWebhookCommandHandlerTests.cs` |
| Test   | `tests/CRM.Application.Tests/Webhooks/ProcessSmsWebhookCommandHandlerTests.cs` |

---

## Task 1: WhatsApp & SMS Inbound Processing

> Note: `WebhooksController` is from US-BE-088. `ICustomerRepository`, `ITicketRepository` are from US-BE-009, US-BE-019. Implement those plans first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Webhooks/ProcessWhatsAppWebhookCommandHandlerTests.cs
using CRM.Application.Webhooks.Commands;
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Webhooks;

public class ProcessWhatsAppWebhookCommandHandlerTests
{
    private readonly Mock<ITwilioSignatureValidator> _validator = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly ProcessWhatsAppWebhookCommandHandler _handler;

    public ProcessWhatsAppWebhookCommandHandlerTests()
    {
        _handler = new ProcessWhatsAppWebhookCommandHandler(
            _validator.Object, _tickets.Object, _customers.Object);
    }

    [Fact]
    public async Task Handle_InvalidSignature_ThrowsUnauthorizedAccessException()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>())).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ProcessWhatsAppWebhookCommand(
                    "badsig", "https://example.com",
                    "whatsapp:+15005550006", "Hello", "+15005550006",
                    "SM123", null, 0),
                default));
    }

    [Fact]
    public async Task Handle_NewSender_AutoCreatesCustomerAndTicket()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>())).Returns(true);
        _customers.Setup(r => r.FindByPhoneAsync("+15005550006", default))
                  .ReturnsAsync((Customer?)null);
        _tickets.Setup(r => r.FindOpenByCustomerPhoneAsync("+15005550006", default))
                .ReturnsAsync((Ticket?)null);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("SM123", default))
                .ReturnsAsync((Ticket?)null);

        await _handler.Handle(
            new ProcessWhatsAppWebhookCommand(
                "sig", "https://example.com",
                "whatsapp:+15005550006", "Hello", "Alice",
                "SM123", null, 0),
            default);

        _customers.Verify(r => r.AddAsync(It.IsAny<Customer>(), default), Times.Once);
        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_WhatsAppPrefix_StrippedBeforePhoneLookup()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>())).Returns(true);
        _customers.Setup(r => r.FindByPhoneAsync("+15005550006", default))
                  .ReturnsAsync((Customer?)null);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("SM456", default))
                .ReturnsAsync((Ticket?)null);
        _tickets.Setup(r => r.FindOpenByCustomerPhoneAsync("+15005550006", default))
                .ReturnsAsync((Ticket?)null);

        await _handler.Handle(
            new ProcessWhatsAppWebhookCommand(
                "sig", "https://example.com",
                "whatsapp:+15005550006", "Hi", "Bob",
                "SM456", null, 0),
            default);

        _customers.Verify(r => r.FindByPhoneAsync("+15005550006", default), Times.Once);
    }
}
```

```csharp
// tests/CRM.Application.Tests/Webhooks/ProcessSmsWebhookCommandHandlerTests.cs
using CRM.Application.Webhooks.Commands;
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Webhooks;

public class ProcessSmsWebhookCommandHandlerTests
{
    private readonly Mock<ITwilioSignatureValidator> _validator = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly ProcessSmsWebhookCommandHandler _handler;

    public ProcessSmsWebhookCommandHandlerTests()
    {
        _handler = new ProcessSmsWebhookCommandHandler(
            _validator.Object, _tickets.Object, _customers.Object);
    }

    [Fact]
    public async Task Handle_DuplicateMessageSid_SkipsProcessing()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>())).Returns(true);

        var existingTicket = Ticket.Create("Test", Guid.NewGuid(), Guid.NewGuid(), "SMS");
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("SM-DUP", default))
                .ReturnsAsync(existingTicket);

        await _handler.Handle(
            new ProcessSmsWebhookCommand(
                "sig", "https://example.com",
                "+15005550006", "Hello again", "SM-DUP"),
            default);

        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_NewSms_CreatesTicket()
    {
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>())).Returns(true);
        _tickets.Setup(r => r.FindByExternalMessageIdAsync("SM-NEW", default))
                .ReturnsAsync((Ticket?)null);
        _customers.Setup(r => r.FindByPhoneAsync("+15005550006", default))
                  .ReturnsAsync((Customer?)null);
        _tickets.Setup(r => r.FindOpenByCustomerPhoneAsync("+15005550006", default))
                .ReturnsAsync((Ticket?)null);

        await _handler.Handle(
            new ProcessSmsWebhookCommand(
                "sig", "https://example.com",
                "+15005550006", "First message", "SM-NEW"),
            default);

        _tickets.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ProcessWhatsAppWebhookCommandHandlerTests|ProcessSmsWebhookCommandHandlerTests" -v n
```

Expected: FAIL — types do not exist yet.

- [ ] **Step 3: Create ITwilioSignatureValidator**

```csharp
// src/CRM.Domain/Channels/ITwilioSignatureValidator.cs
namespace CRM.Domain.Channels;

public interface ITwilioSignatureValidator
{
    bool Validate(string signature, string requestUrl, Dictionary<string, string> postParams);
}
```

- [ ] **Step 4: Add phone lookup methods to ITicketRepository and ICustomerRepository**

Open `src/CRM.Domain/Tickets/ITicketRepository.cs` and add:

```csharp
Task<Ticket?> FindOpenByCustomerPhoneAsync(string phone, CancellationToken ct = default);
```

Open `src/CRM.Domain/Customers/ICustomerRepository.cs` and add:

```csharp
Task<Customer?> FindByPhoneAsync(string phone, CancellationToken ct = default);
```

- [ ] **Step 5: Implement ProcessWhatsAppWebhookCommand**

```csharp
// src/CRM.Application/Webhooks/Commands/ProcessWhatsAppWebhookCommand.cs
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Webhooks.Commands;

public record ProcessWhatsAppWebhookCommand(
    string Signature,
    string RequestUrl,
    string From,              // may be "whatsapp:+E164"
    string Body,
    string ProfileName,       // WhatsApp display name
    string MessageSid,
    string? MediaUrl,
    long MediaSize) : IRequest;

public class ProcessWhatsAppWebhookCommandHandler
    : IRequestHandler<ProcessWhatsAppWebhookCommand>
{
    private const long MaxMediaBytes = 5 * 1024 * 1024;

    private readonly ITwilioSignatureValidator _validator;
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;

    public ProcessWhatsAppWebhookCommandHandler(
        ITwilioSignatureValidator validator,
        ITicketRepository tickets,
        ICustomerRepository customers)
    {
        _validator = validator;
        _tickets = tickets;
        _customers = customers;
    }

    public async Task Handle(ProcessWhatsAppWebhookCommand cmd, CancellationToken ct)
    {
        if (!_validator.Validate(cmd.Signature, cmd.RequestUrl, new Dictionary<string, string>()))
            throw new UnauthorizedAccessException("Invalid Twilio signature.");

        var phone = cmd.From.Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase);

        // Deduplication
        var dup = await _tickets.FindByExternalMessageIdAsync(cmd.MessageSid, ct);
        if (dup is not null) return;

        // Thread match
        var parentTicket = await _tickets.FindOpenByCustomerPhoneAsync(phone, ct);

        var customer = await _customers.FindByPhoneAsync(phone, ct);
        if (customer is null)
        {
            var name = string.IsNullOrWhiteSpace(cmd.ProfileName)
                ? $"Unknown ({phone})" : cmd.ProfileName;
            customer = Customer.Create(name, null, null, phone);
            await _customers.AddAsync(customer, ct);
        }

        if (parentTicket is not null)
        {
            parentTicket.AddMessage("Customer", cmd.Body, cmd.MessageSid, false);
            if (cmd.MediaUrl is not null && cmd.MediaSize > MaxMediaBytes)
                parentTicket.AddMessage("System",
                    $"Media attachment ({cmd.MediaSize / (1024 * 1024)} MB) exceeded 5 MB limit and was dropped.",
                    null, true);
            await _tickets.SaveChangesAsync(ct);
            return;
        }

        var ticket = Ticket.Create("WhatsApp message", customer.Id, Guid.Empty, "WhatsApp");
        ticket.AddMessage("Customer", cmd.Body, cmd.MessageSid, false);
        if (cmd.MediaUrl is not null && cmd.MediaSize > MaxMediaBytes)
            ticket.AddMessage("System",
                $"Media attachment exceeded 5 MB limit and was dropped.", null, true);
        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 6: Implement ProcessSmsWebhookCommand**

```csharp
// src/CRM.Application/Webhooks/Commands/ProcessSmsWebhookCommand.cs
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Webhooks.Commands;

public record ProcessSmsWebhookCommand(
    string Signature,
    string RequestUrl,
    string From,
    string Body,
    string MessageSid) : IRequest;

public class ProcessSmsWebhookCommandHandler : IRequestHandler<ProcessSmsWebhookCommand>
{
    private readonly ITwilioSignatureValidator _validator;
    private readonly ITicketRepository _tickets;
    private readonly ICustomerRepository _customers;

    public ProcessSmsWebhookCommandHandler(
        ITwilioSignatureValidator validator,
        ITicketRepository tickets,
        ICustomerRepository customers)
    {
        _validator = validator;
        _tickets = tickets;
        _customers = customers;
    }

    public async Task Handle(ProcessSmsWebhookCommand cmd, CancellationToken ct)
    {
        if (!_validator.Validate(cmd.Signature, cmd.RequestUrl, new Dictionary<string, string>()))
            throw new UnauthorizedAccessException("Invalid Twilio signature.");

        var dup = await _tickets.FindByExternalMessageIdAsync(cmd.MessageSid, ct);
        if (dup is not null) return;

        var parentTicket = await _tickets.FindOpenByCustomerPhoneAsync(cmd.From, ct);

        var customer = await _customers.FindByPhoneAsync(cmd.From, ct);
        if (customer is null)
        {
            customer = Customer.Create($"Unknown ({cmd.From})", null, null, cmd.From);
            await _customers.AddAsync(customer, ct);
        }

        if (parentTicket is not null)
        {
            parentTicket.AddMessage("Customer", cmd.Body, cmd.MessageSid, false);
            await _tickets.SaveChangesAsync(ct);
            return;
        }

        var ticket = Ticket.Create("SMS message", customer.Id, Guid.Empty, "SMS");
        ticket.AddMessage("Customer", cmd.Body, cmd.MessageSid, false);
        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ProcessWhatsAppWebhookCommandHandlerTests|ProcessSmsWebhookCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Add webhook actions to WebhooksController**

Open `src/CRM.API/Controllers/WebhooksController.cs` and add (inside the class, after the `Email` action):

```csharp
[HttpPost("whatsapp")]
public async Task<IActionResult> WhatsApp(
    [FromHeader(Name = "X-Twilio-Signature")] string signature,
    [FromForm] Dictionary<string, string> form,
    CancellationToken ct)
{
    try
    {
        var cmd = new ProcessWhatsAppWebhookCommand(
            Signature: signature,
            RequestUrl: $"{Request.Scheme}://{Request.Host}{Request.Path}",
            From: form.GetValueOrDefault("From", ""),
            Body: form.GetValueOrDefault("Body", ""),
            ProfileName: form.GetValueOrDefault("ProfileName", ""),
            MessageSid: form.GetValueOrDefault("MessageSid", ""),
            MediaUrl: form.GetValueOrDefault("MediaUrl0"),
            MediaSize: long.TryParse(form.GetValueOrDefault("MediaSize0"), out var ms) ? ms : 0);
        await _mediator.Send(cmd, ct);
    }
    catch (UnauthorizedAccessException) { return StatusCode(403); }
    catch (Exception ex) { _logger.LogError(ex, "WhatsApp webhook error"); }
    return Content("<Response/>", "text/xml");
}

[HttpPost("sms")]
public async Task<IActionResult> Sms(
    [FromHeader(Name = "X-Twilio-Signature")] string signature,
    [FromForm] Dictionary<string, string> form,
    CancellationToken ct)
{
    try
    {
        var cmd = new ProcessSmsWebhookCommand(
            Signature: signature,
            RequestUrl: $"{Request.Scheme}://{Request.Host}{Request.Path}",
            From: form.GetValueOrDefault("From", ""),
            Body: form.GetValueOrDefault("Body", ""),
            MessageSid: form.GetValueOrDefault("MessageSid", ""));
        await _mediator.Send(cmd, ct);
    }
    catch (UnauthorizedAccessException) { return StatusCode(403); }
    catch (Exception ex) { _logger.LogError(ex, "SMS webhook error"); }
    return Content("<Response/>", "text/xml");
}
```

- [ ] **Step 9: Commit**

```bash
git add src/CRM.Domain/Channels/ITwilioSignatureValidator.cs \
        src/CRM.Application/Webhooks/Commands/ProcessWhatsAppWebhookCommand.cs \
        src/CRM.Application/Webhooks/Commands/ProcessSmsWebhookCommand.cs \
        src/CRM.API/Controllers/WebhooksController.cs \
        tests/CRM.Application.Tests/Webhooks/ProcessWhatsAppWebhookCommandHandlerTests.cs \
        tests/CRM.Application.Tests/Webhooks/ProcessSmsWebhookCommandHandlerTests.cs
git commit -m "feat(channels): add POST /webhooks/whatsapp and /webhooks/sms — Twilio signature, dedup, auto-customer"
```
