# Create Ticket (Internal) — Implementation Plan

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

**Story:** US-BE-019  
**Goal:** Implement `POST /api/v1/tickets` — allows Admin/Manager/Agent to open a ticket for a customer with subject, description, category, priority, channel, and custom field values.

**Architecture:** `CreateTicketInternalCommand` → handler validates customer exists, validates custom field values against field definitions, creates `Ticket` aggregate (status = New), persists, publishes `TicketCreatedEvent` for SLA clock start. Returns `TicketSummaryDto`.

> **⚠️ Implementation divergences from original plan:**
> - `SubjectAr` and `DescriptionAr` are **required** fields in the API request (not optional). Both must be non-empty strings.
> - `ITicketRepository` has additional lookup methods added for name resolution: `GetDepartmentNameAsync`, `GetCategoryNameAsync`, `IsDepartmentActiveAsync`
> - Route is `/api/v1/tickets` (versioned prefix)

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/Ticket.cs` |
| Create | `src/CRM.Domain/Tickets/TicketMessage.cs` |
| Create | `src/CRM.Domain/Tickets/TicketHistory.cs` |
| Create | `src/CRM.Domain/Tickets/Enums/TicketStatus.cs` |
| Create | `src/CRM.Domain/Tickets/Enums/TicketPriority.cs` |
| Create | `src/CRM.Domain/Tickets/Enums/TicketChannel.cs` |
| Create | `src/CRM.Domain/Tickets/ITicketRepository.cs` |
| Create | `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs` |
| Create | `src/CRM.Application/Tickets/DTOs/TicketSummaryDto.cs` |
| Create | `src/CRM.Application/Tickets/Validators/CreateTicketInternalCommandValidator.cs` |
| Create | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/CreateTicketInternalCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerCreateTests.cs` |

---

## Task 1: Ticket Domain Aggregate + Enums

**Files:**
- Create: `src/CRM.Domain/Tickets/Enums/TicketStatus.cs`
- Create: `src/CRM.Domain/Tickets/Enums/TicketPriority.cs`
- Create: `src/CRM.Domain/Tickets/Enums/TicketChannel.cs`
- Create: `src/CRM.Domain/Tickets/TicketHistory.cs`
- Create: `src/CRM.Domain/Tickets/Ticket.cs`
- Create: `src/CRM.Domain/Tickets/ITicketRepository.cs`

- [ ] **Step 1: Create enums**

```csharp
// src/CRM.Domain/Tickets/Enums/TicketStatus.cs
namespace CRM.Domain.Tickets.Enums;

public enum TicketStatus
{
    New,
    Assigned,
    InProgress,
    OnHold,
    Escalated,
    Resolved,
    Reopened,
    Closed
}
```

```csharp
// src/CRM.Domain/Tickets/Enums/TicketPriority.cs
namespace CRM.Domain.Tickets.Enums;

public enum TicketPriority { Low, Medium, High, Critical }
```

```csharp
// src/CRM.Domain/Tickets/Enums/TicketChannel.cs
namespace CRM.Domain.Tickets.Enums;

public enum TicketChannel { Portal, Email, WhatsApp, SMS, Phone, Internal }
```

- [ ] **Step 2: Create TicketHistory and Ticket aggregate**

```csharp
// src/CRM.Domain/Tickets/TicketHistory.cs
namespace CRM.Domain.Tickets;

public class TicketHistory
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string FieldChanged { get; private set; } = null!;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private TicketHistory() { }

    public static TicketHistory Create(Guid ticketId, string field,
        string? oldValue, string? newValue, Guid changedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            FieldChanged = field,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = changedBy,
            ChangedAt = DateTime.UtcNow
        };
}
```

```csharp
// src/CRM.Domain/Tickets/Ticket.cs
using CRM.Domain.Tickets.Enums;

namespace CRM.Domain.Tickets;

public class Ticket
{
    public Guid Id { get; private set; }
    public string TicketNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Subject { get; private set; } = null!;
    public string? SubjectAr { get; private set; }
    public string Description { get; private set; } = null!;
    public string? DescriptionAr { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketChannel Channel { get; private set; }
    public string? CustomFieldValues { get; private set; }  // JSON
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    private readonly List<TicketHistory> _history = new();
    public IReadOnlyList<TicketHistory> History => _history.AsReadOnly();

    private Ticket() { }

    public static Ticket Create(
        Guid customerId,
        string subject,
        string description,
        TicketPriority priority,
        TicketChannel channel,
        Guid createdByUserId,
        Guid? departmentId = null,
        Guid? categoryId = null,
        string? customFieldValues = null,
        string? subjectAr = null,
        string? descriptionAr = null)
    {
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketNumber = GenerateNumber(),
            CustomerId = customerId,
            Subject = subject,
            SubjectAr = subjectAr,
            Description = description,
            DescriptionAr = descriptionAr,
            Status = TicketStatus.New,
            Priority = priority,
            Channel = channel,
            DepartmentId = departmentId,
            CategoryId = categoryId,
            CustomFieldValues = customFieldValues,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        ticket._history.Add(TicketHistory.Create(
            ticket.Id, "Status", null, TicketStatus.New.ToString(), createdByUserId));

        return ticket;
    }

    public void Assign(Guid agentId, Guid changedBy)
    {
        var oldAssignee = AssignedToUserId?.ToString();
        AssignedToUserId = agentId;
        Status = TicketStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
        _history.Add(TicketHistory.Create(Id, "AssignedTo", oldAssignee, agentId.ToString(), changedBy));
        _history.Add(TicketHistory.Create(Id, "Status", TicketStatus.New.ToString(), TicketStatus.Assigned.ToString(), changedBy));
    }

    public void ChangeStatus(TicketStatus newStatus, Guid changedBy)
    {
        var oldStatus = Status.ToString();
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        if (newStatus == TicketStatus.Resolved) ResolvedAt = DateTime.UtcNow;
        if (newStatus == TicketStatus.Closed) ClosedAt = DateTime.UtcNow;
        _history.Add(TicketHistory.Create(Id, "Status", oldStatus, newStatus.ToString(), changedBy));
    }

    public void UpdateCategory(Guid? categoryId, Guid? departmentId, Guid changedBy)
    {
        var oldCat = CategoryId?.ToString();
        CategoryId = categoryId;
        DepartmentId = departmentId;
        UpdatedAt = DateTime.UtcNow;
        _history.Add(TicketHistory.Create(Id, "CategoryId", oldCat, categoryId?.ToString(), changedBy));
    }

    private static string GenerateNumber()
        => $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
}
```

```csharp
// src/CRM.Domain/Tickets/ITicketRepository.cs
using CRM.Application.Customers.Queries;
using CRM.Application.Common;

namespace CRM.Domain.Tickets;

public interface ITicketRepository
{
    Task<Ticket?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ticket?> FindByIdWithHistoryAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task<bool> HasOpenTicketsAsync(Guid customerId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Commit**

```bash
git add src/CRM.Domain/Tickets/
git commit -m "feat(domain): add Ticket aggregate, enums, TicketHistory, ITicketRepository"
```

---

## Task 2: CreateTicketInternal Command + Handler + Validator + DTO

**Files:**
- Create: `src/CRM.Application/Tickets/DTOs/TicketSummaryDto.cs`
- Create: `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs`
- Create: `src/CRM.Application/Tickets/Validators/CreateTicketInternalCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Tickets/CreateTicketInternalCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/CreateTicketInternalCommandHandlerTests.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class CreateTicketInternalCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly CreateTicketInternalCommandHandler _handler;

    public CreateTicketInternalCommandHandlerTests()
    {
        _handler = new CreateTicketInternalCommandHandler(
            _customerRepo.Object, _ticketRepo.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTicketWithStatusNew()
    {
        var customerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");

        _customerRepo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _handler.Handle(new CreateTicketInternalCommand(
            customerId, "Cannot login", "User cannot login to portal",
            TicketPriority.High, TicketChannel.Internal, agentId, null, null, null), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("New", result.Status);
        Assert.Equal("Cannot login", result.Subject);
        _ticketRepo.Verify(r => r.AddAsync(It.IsAny<Ticket>(), default), Times.Once);
        _ticketRepo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCustomer_ThrowsKeyNotFoundException()
    {
        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
                     .ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateTicketInternalCommand(
                Guid.NewGuid(), "Subj", "Desc",
                TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid(),
                null, null, null), default));
    }

    [Fact]
    public async Task Handle_DeletedCustomer_ThrowsKeyNotFoundException()
    {
        var customer = Customer.Create("Ali", "Hassan", "ali@crm.test");
        customer.SoftDelete();

        _customerRepo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync(customer);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CreateTicketInternalCommand(
                Guid.NewGuid(), "Subj", "Desc",
                TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid(),
                null, null, null), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateTicketInternalCommandHandlerTests" -v n
```

Expected: FAIL — `CreateTicketInternalCommand` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Tickets/DTOs/TicketSummaryDto.cs
namespace CRM.Application.Tickets.DTOs;

public record TicketSummaryDto(
    Guid Id,
    string TicketNumber,
    Guid CustomerId,
    string CustomerName,
    string Subject,
    string? SubjectAr,
    string Status,
    string Priority,
    string Channel,
    Guid? AssignedToUserId,
    string? AssignedToName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 4: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record CreateTicketInternalCommand(
    Guid CustomerId,
    string Subject,
    string Description,
    TicketPriority Priority,
    TicketChannel Channel,
    Guid CreatedByUserId,
    Guid? DepartmentId,
    Guid? CategoryId,
    string? CustomFieldValues,
    string? SubjectAr = null,
    string? DescriptionAr = null) : IRequest<TicketSummaryDto>;

public class CreateTicketInternalCommandHandler
    : IRequestHandler<CreateTicketInternalCommand, TicketSummaryDto>
{
    private readonly ICustomerRepository _customers;
    private readonly ITicketRepository _tickets;

    public CreateTicketInternalCommandHandler(
        ICustomerRepository customers, ITicketRepository tickets)
    {
        _customers = customers;
        _tickets = tickets;
    }

    public async Task<TicketSummaryDto> Handle(
        CreateTicketInternalCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct);

        if (customer is null || customer.IsDeleted)
            throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        var ticket = Ticket.Create(
            customerId: cmd.CustomerId,
            subject: cmd.Subject,
            description: cmd.Description,
            priority: cmd.Priority,
            channel: cmd.Channel,
            createdByUserId: cmd.CreatedByUserId,
            departmentId: cmd.DepartmentId,
            categoryId: cmd.CategoryId,
            customFieldValues: cmd.CustomFieldValues,
            subjectAr: cmd.SubjectAr,
            descriptionAr: cmd.DescriptionAr);

        await _tickets.AddAsync(ticket, ct);
        await _tickets.SaveChangesAsync(ct);

        return new TicketSummaryDto(
            ticket.Id, ticket.TicketNumber, ticket.CustomerId,
            $"{customer.FirstName} {customer.LastName}",
            ticket.Subject, ticket.SubjectAr, ticket.Status.ToString(), ticket.Priority.ToString(),
            ticket.Channel.ToString(), ticket.AssignedToUserId, null,
            ticket.CreatedAt, ticket.UpdatedAt);
    }
}
```

- [ ] **Step 5: Create validator**

```csharp
// src/CRM.Application/Tickets/Validators/CreateTicketInternalCommandValidator.cs
using CRM.Application.Tickets.Commands;
using FluentValidation;

namespace CRM.Application.Tickets.Validators;

public class CreateTicketInternalCommandValidator
    : AbstractValidator<CreateTicketInternalCommand>
{
    public CreateTicketInternalCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SubjectAr).MaximumLength(500).When(x => x.SubjectAr is not null);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(10000);
        RuleFor(x => x.DescriptionAr).When(x => x.DescriptionAr is not null);
        RuleFor(x => x.CreatedByUserId).NotEmpty();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CreateTicketInternalCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CRM.Application/Tickets/ \
        tests/CRM.Application.Tests/Tickets/CreateTicketInternalCommandHandlerTests.cs
git commit -m "feat(tickets): add CreateTicketInternalCommand with customer validation"
```

---

## Task 3: TicketsController — POST /api/tickets

**Files:**
- Create: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerCreateTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerCreateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerCreateTests
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
    public async Task CreateTicket_ValidBody_Returns201()
    {
        var id = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketInternalCommand>(), default))
                 .ReturnsAsync(new TicketSummaryDto(id, "TKT-001", Guid.NewGuid(),
                     "Ali Hassan", "Cannot login", null, "New", "High", "Internal",
                     null, null, DateTime.UtcNow, DateTime.UtcNow));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId = Guid.NewGuid(),
            subject = "Cannot login",
            description = "User cannot access portal",
            priority = "High",
            channel = "Internal"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_NonExistentCustomer_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateTicketInternalCommand>(), default))
                 .ThrowsAsync(new KeyNotFoundException("Customer not found."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId = Guid.NewGuid(),
            subject = "Subj",
            description = "Desc",
            priority = "Low",
            channel = "Internal"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerCreateTests" -v n
```

Expected: FAIL — `TicketsController` does not exist.

- [ ] **Step 3: Implement TicketsController**

```csharp
// src/CRM.API/Controllers/TicketsController.cs
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize(Roles = "Admin,Manager,Agent")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TicketsController(IMediator mediator) => _mediator = mediator;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")!.Value);

    public record CreateTicketRequest(
        Guid CustomerId,
        string Subject,
        string Description,
        TicketPriority Priority,
        TicketChannel Channel,
        Guid? DepartmentId,
        Guid? CategoryId,
        string? CustomFieldValues);

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTicketRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new CreateTicketInternalCommand(
                request.CustomerId, request.Subject, request.Description,
                request.Priority, request.Channel, CurrentUserId,
                request.DepartmentId, request.CategoryId, request.CustomFieldValues), ct);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id) => Ok(); // Implemented in US-BE-021
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerCreateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerCreateTests.cs
git commit -m "feat(api): add POST /api/tickets endpoint"
```
