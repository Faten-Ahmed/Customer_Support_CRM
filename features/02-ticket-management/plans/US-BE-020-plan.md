# Create Ticket (Portal) — Implementation Plan

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

**Story:** US-BE-020  
**Goal:** Implement `POST /api/portal/tickets` — allows a verified customer to submit a ticket from the portal. Channel is always `Portal`; `customerId` is taken from the JWT claim (no impersonation).

**Architecture:** `CreateTicketPortalCommand(subject, description, priority, categoryId, customFieldValues, portalCustomerId)` → handler validates customer is verified and active, creates `Ticket` with `Channel = Portal`, persists. Separate command from internal to enforce channel and ownership invariants.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Portal/Tickets/Commands/CreateTicketPortalCommand.cs` |
| Create | `src/CRM.Application/Portal/Tickets/Validators/CreateTicketPortalCommandValidator.cs` |
| Create | `src/CRM.API/Controllers/Portal/PortalTicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Portal/Tickets/CreateTicketPortalCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Portal/PortalTicketsControllerCreateTests.cs` |

---

## Task 1: CreateTicketPortal Command + Handler + Validator

**Files:**
- Create: `src/CRM.Application/Portal/Tickets/Commands/CreateTicketPortalCommand.cs`
- Create: `src/CRM.Application/Portal/Tickets/Validators/CreateTicketPortalCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Portal/Tickets/CreateTicketPortalCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Portal/Tickets/CreateTicketPortalCommandHandlerTests.cs
using CRM.Application.Portal.Tickets.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal.Tickets;

public class CreateTicketPortalCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ICustomerCredentialRepository> _credRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly CreateTicketPortalCommandHandler _handler;

    public CreateTicketPortalCommandHandlerTests()
    {
        _handler = new CreateTicketPortalCommandHandler(
            _customerRepo.Object, _credRepo.Object, _ticketRepo.Object);
    }

    [Fact]
    public async Task Handle_VerifiedCustomer_CreatesTicketWithPortalChannel()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@portal.test");
        var cred = CustomerCredential.Create(customerId, "hash");
        cred.VerifyEmail();

        _customerRepo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _credRepo.Setup(r => r.FindByCustomerIdAsync(customerId, default)).ReturnsAsync(cred);

        Ticket? captured = null;
        _ticketRepo.Setup(r => r.AddAsync(It.IsAny<Ticket>(), default))
                   .Callback<Ticket, CancellationToken>((t, _) => captured = t)
                   .Returns(Task.CompletedTask);

        await _handler.Handle(new CreateTicketPortalCommand(
            "My screen is black", "Description here", TicketPriority.Medium,
            null, null, customerId), default);

        Assert.NotNull(captured);
        Assert.Equal(TicketChannel.Portal, captured!.Channel);
        Assert.Equal(TicketStatus.New, captured.Status);
    }

    [Fact]
    public async Task Handle_UnverifiedEmail_ThrowsUnauthorizedAccessException()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@portal.test");
        var cred = CustomerCredential.Create(customerId, "hash"); // Not verified

        _customerRepo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);
        _credRepo.Setup(r => r.FindByCustomerIdAsync(customerId, default)).ReturnsAsync(cred);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new CreateTicketPortalCommand(
                "Subj", "Desc", TicketPriority.Low, null, null, customerId), default));
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateTicketPortalCommand(
                "Subj", "Desc", TicketPriority.Low, null, null, Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateTicketPortalCommandHandlerTests" -v n
```

Expected: FAIL — `CreateTicketPortalCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Portal/Tickets/Commands/CreateTicketPortalCommand.cs
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Portal.Tickets.Commands;

public record CreateTicketPortalCommand(
    string Subject,
    string Description,
    TicketPriority Priority,
    Guid? CategoryId,
    string? CustomFieldValues,
    Guid PortalCustomerId) : IRequest<TicketSummaryDto>;

public class CreateTicketPortalCommandHandler
    : IRequestHandler<CreateTicketPortalCommand, TicketSummaryDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ICustomerCredentialRepository _credentials;
    private readonly ITicketRepository _tickets;

    public CreateTicketPortalCommandHandler(
        ICustomerRepository customers,
        ICustomerCredentialRepository credentials,
        ITicketRepository tickets)
    {
        _customers = customers;
        _credentials = credentials;
        _tickets = tickets;
    }

    public async Task<TicketSummaryDto> Handle(
        CreateTicketPortalCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.PortalCustomerId, ct);
        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException("Customer not found.");

        var credential = await _credentials.FindByCustomerIdAsync(cmd.PortalCustomerId, ct);
        if (credential is null || !credential.IsEmailVerified)
            throw new UnauthorizedAccessException("Email not verified. Please verify your email first.");

        var ticket = Ticket.Create(
            customerId: cmd.PortalCustomerId,
            subject: cmd.Subject,
            description: cmd.Description,
            priority: cmd.Priority,
            channel: TicketChannel.Portal,
            createdByUserId: cmd.PortalCustomerId,
            categoryId: cmd.CategoryId,
            customFieldValues: cmd.CustomFieldValues);

        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);

        return new TicketSummaryDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId,
            $"{customer.FirstName} {customer.LastName}",
            ticket.Subject, ticket.Status.ToString(), ticket.Priority.ToString(),
            ticket.Channel.ToString(), null, null, ticket.CreatedAt, ticket.UpdatedAt);
    }
}
```

- [ ] **Step 4: Create validator**

```csharp
// src/CRM.Application/Portal/Tickets/Validators/CreateTicketPortalCommandValidator.cs
using CRM.Application.Portal.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Portal.Tickets.Validators;

public class CreateTicketPortalCommandValidator
    : AbstractValidator<CreateTicketPortalCommand>
{
    public CreateTicketPortalCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.PortalCustomerId).NotEmpty();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateTicketPortalCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Portal/Tickets/ \
        tests/CRM.Application.Tests/Portal/Tickets/CreateTicketPortalCommandHandlerTests.cs
git commit -m "feat(portal): add CreateTicketPortalCommand with email-verified guard"
```

---

## Task 2: PortalTicketsController — POST /api/portal/tickets

**Files:**
- Create: `src/CRM.API/Controllers/Portal/PortalTicketsController.cs`
- Test: `tests/CRM.API.Tests/Portal/PortalTicketsControllerCreateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Portal/PortalTicketsControllerCreateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Portal.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalTicketsControllerCreateTests
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
                "Bearer", TestJwtHelper.CreatePortalCustomerToken());
        return client;
    }

    [Fact]
    public async Task CreatePortalTicket_ValidBody_Returns201()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketPortalCommand>(), default))
                 .ReturnsAsync(new TicketSummaryDto(id, "TKT-001", Guid.NewGuid(),
                     "Ali Hassan", "Screen black", "New", "Medium", "Portal",
                     null, null, DateTime.UtcNow, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/portal/tickets",
            new { subject = "Screen black", description = "My screen is black", priority = "Medium" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePortalTicket_UnverifiedEmail_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketPortalCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Email not verified."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/portal/tickets",
            new { subject = "Subj", description = "Desc", priority = "Low" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalTicketsControllerCreateTests" -v n
```

Expected: FAIL — `PortalTicketsController` does not exist.

- [ ] **Step 3: Implement PortalTicketsController**

```csharp
// src/CRM.API/Controllers/Portal/PortalTicketsController.cs
using CRM.Application.Portal.Tickets.Commands;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers.Portal;

[ApiController]
[Route("api/portal/tickets")]
[Authorize(Policy = "PortalCustomer")]
public class PortalTicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PortalTicketsController(IMediator mediator) => _mediator = mediator;

    private Guid CustomerId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    public record CreatePortalTicketRequest(
        string Subject, string Description, TicketPriority Priority,
        Guid? CategoryId, string? CustomFieldValues);

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePortalTicketRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateTicketPortalCommand(
                request.Subject, request.Description, request.Priority,
                request.CategoryId, request.CustomFieldValues, CustomerId), ct);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => Ok(); // Stub — implemented in portal list story
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalTicketsControllerCreateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/Portal/PortalTicketsController.cs \
        tests/CRM.API.Tests/Portal/PortalTicketsControllerCreateTests.cs
git commit -m "feat(api): add POST /api/portal/tickets endpoint"
```
