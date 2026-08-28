# SLA Clock Start on Ticket Creation — Implementation Plan

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

**Story:** US-BE-039  
**Goal:** When a ticket is created, look up the applicable `SlaPolicy` (department + priority first, then global + priority), snapshot its values onto a `TicketSla` record, and calculate `FirstResponseDeadlineUtc` and `ResolutionDeadlineUtc` using business-hours-aware time arithmetic.

**Architecture:** `StartSlaClockCommandHandler` is enqueued (or called inline) after ticket creation. It loads `SlaPolicy` from `ISlaPolicyRepository`, loads `BusinessHours` from `IBusinessHoursRepository`, computes deadlines via `BusinessTimeCalculator.AddBusinessMinutes`, creates a `TicketSla` record via `ITicketSlaRepository`, and saves.

**Note:** `SlaPolicy` entity is defined in US-BE-043-plan; `BusinessHours`/`Holiday` are defined in US-BE-044-plan. Implement those two plans before this one.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Sla/SlaPolicy.cs` |
| Create | `src/CRM.Domain/Sla/ISlaPolicyRepository.cs` |
| Create | `src/CRM.Domain/Sla/BusinessHours.cs` |
| Create | `src/CRM.Domain/Sla/IBusinessHoursRepository.cs` |
| Create | `src/CRM.Application/Sla/BusinessTimeCalculator.cs` |
| Create | `src/CRM.Application/Sla/Commands/StartSlaClockCommand.cs` |
| Modify | `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/BusinessTimeCalculatorTests.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/StartSlaClockCommandHandlerTests.cs` |

---

## Task 1: Domain Entities

**Files:**
- Create: `src/CRM.Domain/Sla/SlaPolicy.cs`
- Create: `src/CRM.Domain/Sla/ISlaPolicyRepository.cs`
- Create: `src/CRM.Domain/Sla/BusinessHours.cs`
- Create: `src/CRM.Domain/Sla/IBusinessHoursRepository.cs`

- [ ] **Step 1: Create SlaPolicy entity**

```csharp
// src/CRM.Domain/Sla/SlaPolicy.cs
using CRM.Domain.Tickets.Enums;

namespace CRM.Domain.Sla;

public class SlaPolicy
{
    public Guid Id { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public TicketPriority Priority { get; private set; }
    public int FirstResponseMinutes { get; private set; }
    public int ResolutionMinutes { get; private set; }
    public int WarningThresholdPercent { get; private set; }
    public int BreachThresholdPercent { get; private set; }
    public int CriticalBreachThresholdPercent { get; private set; }

    private SlaPolicy() { }

    public static SlaPolicy Create(
        TicketPriority priority,
        int firstResponseMinutes,
        int resolutionMinutes,
        Guid? departmentId = null,
        int warningThresholdPercent = 80,
        int breachThresholdPercent = 100,
        int criticalBreachThresholdPercent = 200)
        => new()
        {
            Id = Guid.NewGuid(),
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            DepartmentId = departmentId,
            WarningThresholdPercent = warningThresholdPercent,
            BreachThresholdPercent = breachThresholdPercent,
            CriticalBreachThresholdPercent = criticalBreachThresholdPercent
        };

    public void Update(
        int firstResponseMinutes, int resolutionMinutes,
        int warningPercent, int breachPercent, int criticalPercent)
    {
        FirstResponseMinutes = firstResponseMinutes;
        ResolutionMinutes = resolutionMinutes;
        WarningThresholdPercent = warningPercent;
        BreachThresholdPercent = breachPercent;
        CriticalBreachThresholdPercent = criticalPercent;
    }
}
```

```csharp
// src/CRM.Domain/Sla/ISlaPolicyRepository.cs
using CRM.Domain.Tickets.Enums;

namespace CRM.Domain.Sla;

public interface ISlaPolicyRepository
{
    Task<SlaPolicy?> FindByDepartmentAndPriorityAsync(
        Guid departmentId, TicketPriority priority, CancellationToken ct = default);

    Task<SlaPolicy?> FindGlobalByPriorityAsync(
        TicketPriority priority, CancellationToken ct = default);

    Task<IReadOnlyList<SlaPolicy>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(SlaPolicy policy, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create BusinessHours entity**

```csharp
// src/CRM.Domain/Sla/BusinessHours.cs
namespace CRM.Domain.Sla;

public class BusinessHours
{
    public Guid Id { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public string[] WorkDays { get; private set; } = null!;
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string TimeZone { get; private set; } = null!;

    private readonly List<Holiday> _holidays = new();
    public IReadOnlyList<Holiday> Holidays => _holidays.AsReadOnly();

    private BusinessHours() { }

    public static BusinessHours Create(
        string[] workDays, TimeOnly startTime, TimeOnly endTime,
        string timeZone, Guid? departmentId = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WorkDays = workDays,
            StartTime = startTime,
            EndTime = endTime,
            TimeZone = timeZone,
            DepartmentId = departmentId
        };

    public void Update(string[] workDays, TimeOnly startTime, TimeOnly endTime, string timeZone)
    {
        WorkDays = workDays;
        StartTime = startTime;
        EndTime = endTime;
        TimeZone = timeZone;
    }

    public Holiday AddHoliday(DateOnly date, string name)
    {
        if (_holidays.Any(h => h.Date == date))
            throw new InvalidOperationException($"Holiday already exists on {date:yyyy-MM-dd}.");
        var holiday = Holiday.Create(Id, date, name);
        _holidays.Add(holiday);
        return holiday;
    }

    public void RemoveHoliday(Guid holidayId)
    {
        var holiday = _holidays.FirstOrDefault(h => h.Id == holidayId)
            ?? throw new KeyNotFoundException($"Holiday {holidayId} not found.");
        _holidays.Remove(holiday);
    }
}

public class Holiday
{
    public Guid Id { get; private set; }
    public Guid BusinessHoursId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = null!;

    private Holiday() { }

    public static Holiday Create(Guid businessHoursId, DateOnly date, string name)
        => new() { Id = Guid.NewGuid(), BusinessHoursId = businessHoursId, Date = date, Name = name };
}
```

```csharp
// src/CRM.Domain/Sla/IBusinessHoursRepository.cs
namespace CRM.Domain.Sla;

public interface IBusinessHoursRepository
{
    Task<BusinessHours?> FindByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
    Task<BusinessHours?> FindGlobalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<BusinessHours>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(BusinessHours businessHours, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 3: Commit domain entities**

```bash
git add src/CRM.Domain/Sla/
git commit -m "feat(domain): add SlaPolicy and BusinessHours entities with repository interfaces"
```

---

## Task 2: BusinessTimeCalculator

**Files:**
- Create: `src/CRM.Application/Sla/BusinessTimeCalculator.cs`
- Test: `tests/CRM.Application.Tests/Sla/BusinessTimeCalculatorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/BusinessTimeCalculatorTests.cs
using CRM.Application.Sla;
using CRM.Domain.Sla;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class BusinessTimeCalculatorTests
{
    private static BusinessHours MakeWeekdayHours(string tz = "UTC")
        => BusinessHours.Create(
            new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            new TimeOnly(9, 0), new TimeOnly(17, 0), tz);

    [Fact]
    public void AddBusinessMinutes_WithinBusinessDay_AddsCorrectly()
    {
        var hours = MakeWeekdayHours();
        var start = new DateTime(2025, 1, 6, 9, 0, 0, DateTimeKind.Utc); // Monday 9:00

        var result = BusinessTimeCalculator.AddBusinessMinutes(start, 60, hours);

        Assert.Equal(new DateTime(2025, 1, 6, 10, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void AddBusinessMinutes_SpansEndOfDay_WrapsToNextDay()
    {
        var hours = MakeWeekdayHours();
        var start = new DateTime(2025, 1, 6, 16, 30, 0, DateTimeKind.Utc); // Monday 16:30

        var result = BusinessTimeCalculator.AddBusinessMinutes(start, 60, hours);

        // 30 min remaining today (16:30-17:00) + 30 min next day (9:00-9:30)
        Assert.Equal(new DateTime(2025, 1, 7, 9, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void AddBusinessMinutes_SpansWeekend_SkipsWeekend()
    {
        var hours = MakeWeekdayHours();
        var start = new DateTime(2025, 1, 10, 16, 30, 0, DateTimeKind.Utc); // Friday 16:30

        var result = BusinessTimeCalculator.AddBusinessMinutes(start, 60, hours);

        // Skips Saturday + Sunday, continues Monday
        Assert.Equal(new DateTime(2025, 1, 13, 9, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void AddBusinessMinutes_StartOutsideBusinessHours_StartsFromNextBusinessMinute()
    {
        var hours = MakeWeekdayHours();
        var start = new DateTime(2025, 1, 6, 6, 0, 0, DateTimeKind.Utc); // Monday 6:00 (before hours)

        var result = BusinessTimeCalculator.AddBusinessMinutes(start, 30, hours);

        Assert.Equal(new DateTime(2025, 1, 6, 9, 30, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void AddBusinessMinutes_HolidaySkipped()
    {
        var hours = MakeWeekdayHours();
        hours.AddHoliday(new DateOnly(2025, 1, 6), "Test Holiday"); // Monday is holiday
        var start = new DateTime(2025, 1, 6, 9, 0, 0, DateTimeKind.Utc);

        var result = BusinessTimeCalculator.AddBusinessMinutes(start, 30, hours);

        // Monday is holiday, so starts Tuesday
        Assert.Equal(new DateTime(2025, 1, 7, 9, 30, 0, DateTimeKind.Utc), result);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "BusinessTimeCalculatorTests" -v n
```

Expected: FAIL — `BusinessTimeCalculator` does not exist yet.

- [ ] **Step 3: Implement BusinessTimeCalculator**

```csharp
// src/CRM.Application/Sla/BusinessTimeCalculator.cs
using CRM.Domain.Sla;

namespace CRM.Application.Sla;

public static class BusinessTimeCalculator
{
    public static DateTime AddBusinessMinutes(DateTime startUtc, int minutes, BusinessHours hours)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(hours.TimeZone);
        var current = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var remaining = minutes;

        while (remaining > 0)
        {
            if (IsBusinessTime(current, hours))
                remaining--;
            current = current.AddMinutes(1);
        }

        // Advance to next business minute if we ended outside business hours
        while (!IsBusinessTime(current, hours))
            current = current.AddMinutes(1);

        return TimeZoneInfo.ConvertTimeToUtc(current, tz);
    }

    public static int ElapsedBusinessMinutes(DateTime startUtc, DateTime endUtc, BusinessHours hours)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(hours.TimeZone);
        var current = TimeZoneInfo.ConvertTimeFromUtc(startUtc, tz);
        var end = TimeZoneInfo.ConvertTimeFromUtc(endUtc, tz);
        var elapsed = 0;

        while (current < end)
        {
            if (IsBusinessTime(current, hours))
                elapsed++;
            current = current.AddMinutes(1);
        }

        return elapsed;
    }

    private static bool IsBusinessTime(DateTime localDt, BusinessHours hours)
    {
        if (!hours.WorkDays.Contains(localDt.DayOfWeek.ToString())) return false;
        var date = DateOnly.FromDateTime(localDt);
        if (hours.Holidays.Any(h => h.Date == date)) return false;
        var time = TimeOnly.FromDateTime(localDt);
        return time >= hours.StartTime && time < hours.EndTime;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "BusinessTimeCalculatorTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Sla/BusinessTimeCalculator.cs \
        tests/CRM.Application.Tests/Sla/BusinessTimeCalculatorTests.cs
git commit -m "feat(sla): add BusinessTimeCalculator with timezone, holiday, and weekend support"
```

---

## Task 3: StartSlaClockCommand + Handler

**Files:**
- Create: `src/CRM.Application/Sla/Commands/StartSlaClockCommand.cs`
- Test: `tests/CRM.Application.Tests/Sla/StartSlaClockCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/StartSlaClockCommandHandlerTests.cs
using CRM.Application.Sla.Commands;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class StartSlaClockCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<ISlaPolicyRepository> _policyRepo = new();
    private readonly Mock<IBusinessHoursRepository> _hoursRepo = new();
    private readonly Mock<ITicketSlaRepository> _slaRepo = new();
    private readonly StartSlaClockCommandHandler _handler;

    public StartSlaClockCommandHandlerTests()
    {
        _handler = new StartSlaClockCommandHandler(
            _ticketRepo.Object, _policyRepo.Object,
            _hoursRepo.Object, _slaRepo.Object);
    }

    private BusinessHours MakeHours()
        => BusinessHours.Create(
            new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            new TimeOnly(9, 0), new TimeOnly(17, 0), "UTC");

    [Fact]
    public async Task Handle_DepartmentPolicyFound_CreatesSlaWithDepartmentPolicy()
    {
        var ticketId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.High, TicketChannel.Email, Guid.NewGuid());

        var policy = SlaPolicy.Create(TicketPriority.High, 60, 480, deptId);
        var hours = MakeHours();

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _policyRepo.Setup(r => r.FindByDepartmentAndPriorityAsync(
            It.IsAny<Guid>(), TicketPriority.High, default)).ReturnsAsync(policy);
        _hoursRepo.Setup(r => r.FindByDepartmentAsync(It.IsAny<Guid>(), default))
                  .ReturnsAsync(hours);

        await _handler.Handle(new StartSlaClockCommand(ticketId), default);

        _slaRepo.Verify(r => r.AddAsync(It.Is<TicketSla>(s =>
            s.TicketId == ticket.Id &&
            s.FirstResponseDue.HasValue &&
            s.ResolutionDue.HasValue), default), Times.Once);
    }

    [Fact]
    public async Task Handle_NoDepartmentPolicy_FallsBackToGlobalPolicy()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());

        var globalPolicy = SlaPolicy.Create(TicketPriority.Low, 240, 1440);
        var hours = MakeHours();

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _policyRepo.Setup(r => r.FindByDepartmentAndPriorityAsync(
            It.IsAny<Guid>(), TicketPriority.Low, default)).ReturnsAsync((SlaPolicy?)null);
        _policyRepo.Setup(r => r.FindGlobalByPriorityAsync(TicketPriority.Low, default))
                   .ReturnsAsync(globalPolicy);
        _hoursRepo.Setup(r => r.FindGlobalAsync(default)).ReturnsAsync(hours);

        await _handler.Handle(new StartSlaClockCommand(ticketId), default);

        _slaRepo.Verify(r => r.AddAsync(It.IsAny<TicketSla>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_NoPolicyExists_DoesNotCreateSlaRecord()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Email, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _policyRepo.Setup(r => r.FindByDepartmentAndPriorityAsync(
            It.IsAny<Guid>(), It.IsAny<TicketPriority>(), default)).ReturnsAsync((SlaPolicy?)null);
        _policyRepo.Setup(r => r.FindGlobalByPriorityAsync(It.IsAny<TicketPriority>(), default))
                   .ReturnsAsync((SlaPolicy?)null);

        await _handler.Handle(new StartSlaClockCommand(ticketId), default);

        _slaRepo.Verify(r => r.AddAsync(It.IsAny<TicketSla>(), default), Times.Never);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "StartSlaClockCommandHandlerTests" -v n
```

Expected: FAIL — `StartSlaClockCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Sla/Commands/StartSlaClockCommand.cs
using CRM.Application.Sla;
using CRM.Domain.Sla;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record StartSlaClockCommand(Guid TicketId) : IRequest;

public class StartSlaClockCommandHandler : IRequestHandler<StartSlaClockCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly ISlaPolicyRepository _policies;
    private readonly IBusinessHoursRepository _businessHours;
    private readonly ITicketSlaRepository _slaRepo;

    public StartSlaClockCommandHandler(
        ITicketRepository tickets,
        ISlaPolicyRepository policies,
        IBusinessHoursRepository businessHours,
        ITicketSlaRepository slaRepo)
    {
        _tickets = tickets;
        _policies = policies;
        _businessHours = businessHours;
        _slaRepo = slaRepo;
    }

    public async Task Handle(StartSlaClockCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        // Resolve policy: department-specific first, then global
        SlaPolicy? policy = null;
        BusinessHours? hours = null;

        if (ticket.DepartmentId.HasValue)
        {
            policy = await _policies.FindByDepartmentAndPriorityAsync(
                ticket.DepartmentId.Value, ticket.Priority, ct);
            hours = await _businessHours.FindByDepartmentAsync(ticket.DepartmentId.Value, ct);
        }

        policy ??= await _policies.FindGlobalByPriorityAsync(ticket.Priority, ct);
        hours ??= await _businessHours.FindGlobalAsync(ct);

        if (policy is null) return; // No SLA configured for this ticket

        var start = ticket.CreatedAt;
        DateTime? firstResponseDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(start, policy.FirstResponseMinutes, hours)
            : start.AddMinutes(policy.FirstResponseMinutes);

        DateTime? resolutionDue = hours is not null
            ? BusinessTimeCalculator.AddBusinessMinutes(start, policy.ResolutionMinutes, hours)
            : start.AddMinutes(policy.ResolutionMinutes);

        var sla = TicketSla.Create(
            ticket.Id, policy.Id, start, firstResponseDue, resolutionDue);

        await _slaRepo.AddAsync(sla, ct);
        await _slaRepo.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Enqueue StartSlaClockCommand from CreateTicketInternalCommandHandler**

In `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs`, after saving the ticket:
```csharp
await _mediator.Send(new StartSlaClockCommand(ticket.Id), ct);
```

Or enqueue as a Hangfire job if isolation is preferred:
```csharp
_jobs.Enqueue<StartSlaClockCommandHandler>(h =>
    h.Handle(new StartSlaClockCommand(ticket.Id), CancellationToken.None));
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "StartSlaClockCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Sla/Commands/StartSlaClockCommand.cs \
        src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs \
        tests/CRM.Application.Tests/Sla/StartSlaClockCommandHandlerTests.cs
git commit -m "feat(sla): add StartSlaClockCommand triggered on ticket creation"
```
