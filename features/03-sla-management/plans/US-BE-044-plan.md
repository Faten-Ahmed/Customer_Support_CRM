# Business Hours & Holiday CRUD — Implementation Plan

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

**Story:** US-BE-044  
**Goal:** Implement CRUD for `BusinessHours` and `Holiday` records under `/api/admin/business-hours` — get/update working days and hours, add/remove holidays. Validates IANA timezone, non-empty workDays, and `startTime < endTime`.

**Architecture:** `GetBusinessHoursQuery` → list all records. `UpdateBusinessHoursCommand` validates and updates. `AddHolidayCommand` calls `businessHours.AddHoliday()` (duplicate date throws `InvalidOperationException` → 409). `DeleteHolidayCommand` calls `businessHours.RemoveHoliday()`.

**Note:** `BusinessHours` and `Holiday` entities are defined in US-BE-039-plan and should already exist.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Sla/DTOs/BusinessHoursDto.cs` |
| Create | `src/CRM.Application/Sla/Queries/GetBusinessHoursQuery.cs` |
| Create | `src/CRM.Application/Sla/Commands/UpdateBusinessHoursCommand.cs` |
| Create | `src/CRM.Application/Sla/Commands/AddHolidayCommand.cs` |
| Create | `src/CRM.Application/Sla/Commands/DeleteHolidayCommand.cs` |
| Create | `src/CRM.API/Controllers/Admin/BusinessHoursController.cs` |
| Test   | `tests/CRM.Application.Tests/Sla/BusinessHoursCrudTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/BusinessHoursControllerTests.cs` |

---

## Task 1: BusinessHours Application Layer

**Files:**
- Create: `src/CRM.Application/Sla/DTOs/BusinessHoursDto.cs`
- Create: `src/CRM.Application/Sla/Queries/GetBusinessHoursQuery.cs`
- Create: `src/CRM.Application/Sla/Commands/UpdateBusinessHoursCommand.cs`
- Create: `src/CRM.Application/Sla/Commands/AddHolidayCommand.cs`
- Create: `src/CRM.Application/Sla/Commands/DeleteHolidayCommand.cs`
- Test: `tests/CRM.Application.Tests/Sla/BusinessHoursCrudTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Sla/BusinessHoursCrudTests.cs
using CRM.Application.Sla.Commands;
using CRM.Application.Sla.Queries;
using CRM.Domain.Sla;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Sla;

public class BusinessHoursCrudTests
{
    private readonly Mock<IBusinessHoursRepository> _repo = new();

    private BusinessHours MakeHours()
        => BusinessHours.Create(
            new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
            new TimeOnly(9, 0), new TimeOnly(17, 0), "UTC");

    [Fact]
    public async Task GetBusinessHours_ReturnsAllRecords()
    {
        _repo.Setup(r => r.ListAllAsync(default))
             .ReturnsAsync(new List<BusinessHours> { MakeHours() });

        var handler = new GetBusinessHoursQueryHandler(_repo.Object);
        var result = await handler.Handle(new GetBusinessHoursQuery(), default);

        Assert.Single(result);
        Assert.Contains("Monday", result[0].WorkDays);
    }

    [Fact]
    public async Task UpdateBusinessHours_ValidData_Persists()
    {
        var hours = MakeHours();
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new UpdateBusinessHoursCommandHandler(_repo.Object);
        await handler.Handle(new UpdateBusinessHoursCommand(
            hours.Id,
            new[] { "Monday", "Wednesday", "Friday" },
            new TimeOnly(8, 0), new TimeOnly(18, 0), "America/New_York"), default);

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
        Assert.Equal(3, hours.WorkDays.Length);
    }

    [Fact]
    public async Task UpdateBusinessHours_EmptyWorkDays_ThrowsValidationException()
    {
        var hours = MakeHours();
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new UpdateBusinessHoursCommandHandler(_repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateBusinessHoursCommand(
                hours.Id, new string[0], new TimeOnly(9, 0), new TimeOnly(17, 0), "UTC"), default));
    }

    [Fact]
    public async Task UpdateBusinessHours_InvalidTimezone_ThrowsValidationException()
    {
        var hours = MakeHours();
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new UpdateBusinessHoursCommandHandler(_repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateBusinessHoursCommand(
                hours.Id, new[] { "Monday" },
                new TimeOnly(9, 0), new TimeOnly(17, 0), "Not/ATimezone"), default));
    }

    [Fact]
    public async Task UpdateBusinessHours_StartAfterEnd_ThrowsValidationException()
    {
        var hours = MakeHours();
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new UpdateBusinessHoursCommandHandler(_repo.Object);

        await Assert.ThrowsAsync<ValidationException>(() =>
            handler.Handle(new UpdateBusinessHoursCommand(
                hours.Id, new[] { "Monday" },
                new TimeOnly(17, 0), new TimeOnly(9, 0), "UTC"), default));
    }

    [Fact]
    public async Task AddHoliday_NewDate_AddsHoliday()
    {
        var hours = MakeHours();
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new AddHolidayCommandHandler(_repo.Object);
        var result = await handler.Handle(new AddHolidayCommand(
            hours.Id, new DateOnly(2025, 12, 25), "Christmas"), default);

        Assert.NotEqual(Guid.Empty, result);
        Assert.Single(hours.Holidays);
    }

    [Fact]
    public async Task AddHoliday_DuplicateDate_ThrowsInvalidOperationException()
    {
        var hours = MakeHours();
        hours.AddHoliday(new DateOnly(2025, 12, 25), "Christmas");
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new AddHolidayCommandHandler(_repo.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AddHolidayCommand(
                hours.Id, new DateOnly(2025, 12, 25), "Christmas Again"), default));
    }

    [Fact]
    public async Task DeleteHoliday_ExistingHoliday_RemovesIt()
    {
        var hours = MakeHours();
        var holiday = hours.AddHoliday(new DateOnly(2025, 12, 25), "Christmas");
        _repo.Setup(r => r.FindByIdAsync(hours.Id, default)).ReturnsAsync(hours);

        var handler = new DeleteHolidayCommandHandler(_repo.Object);
        await handler.Handle(new DeleteHolidayCommand(hours.Id, holiday.Id), default);

        Assert.Empty(hours.Holidays);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "BusinessHoursCrudTests" -v n
```

Expected: FAIL — handlers do not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Sla/DTOs/BusinessHoursDto.cs
namespace CRM.Application.Sla.DTOs;

public record HolidayDto(Guid Id, string Date, string Name);

public record BusinessHoursDto(
    Guid Id,
    Guid? DepartmentId,
    string[] WorkDays,
    string StartTime,
    string EndTime,
    string TimeZone,
    IReadOnlyList<HolidayDto> Holidays);
```

- [ ] **Step 4: Implement query and commands**

```csharp
// src/CRM.Application/Sla/Queries/GetBusinessHoursQuery.cs
using CRM.Application.Sla.DTOs;
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Queries;

public record GetBusinessHoursQuery : IRequest<IReadOnlyList<BusinessHoursDto>>;

public class GetBusinessHoursQueryHandler
    : IRequestHandler<GetBusinessHoursQuery, IReadOnlyList<BusinessHoursDto>>
{
    private readonly IBusinessHoursRepository _repo;
    public GetBusinessHoursQueryHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<BusinessHoursDto>> Handle(
        GetBusinessHoursQuery query, CancellationToken ct)
    {
        var records = await _repo.ListAllAsync(ct);
        return records.Select(h => new BusinessHoursDto(
            h.Id, h.DepartmentId, h.WorkDays,
            h.StartTime.ToString("HH:mm"), h.EndTime.ToString("HH:mm"),
            h.TimeZone,
            h.Holidays.Select(hol => new HolidayDto(
                hol.Id, hol.Date.ToString("yyyy-MM-dd"), hol.Name)).ToList()
        )).ToList();
    }
}
```

```csharp
// src/CRM.Application/Sla/Commands/UpdateBusinessHoursCommand.cs
using CRM.Domain.Sla;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record UpdateBusinessHoursCommand(
    Guid BusinessHoursId,
    string[] WorkDays,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string TimeZone) : IRequest;

public class UpdateBusinessHoursCommandHandler : IRequestHandler<UpdateBusinessHoursCommand>
{
    private readonly IBusinessHoursRepository _repo;
    public UpdateBusinessHoursCommandHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task Handle(UpdateBusinessHoursCommand cmd, CancellationToken ct)
    {
        var bh = await _repo.FindByIdAsync(cmd.BusinessHoursId, ct)
            ?? throw new KeyNotFoundException($"BusinessHours {cmd.BusinessHoursId} not found.");

        var errors = new List<ValidationFailure>();

        if (cmd.WorkDays.Length == 0)
            errors.Add(new ValidationFailure(nameof(cmd.WorkDays),
                "At least one work day is required."));

        if (cmd.StartTime >= cmd.EndTime)
            errors.Add(new ValidationFailure(nameof(cmd.StartTime),
                "Start time must be earlier than end time."));

        try { TimeZoneInfo.FindSystemTimeZoneById(cmd.TimeZone); }
        catch (TimeZoneNotFoundException)
        {
            errors.Add(new ValidationFailure(nameof(cmd.TimeZone),
                $"'{cmd.TimeZone}' is not a valid IANA timezone.",
                "INVALID_TIMEZONE"));
        }

        if (errors.Any()) throw new ValidationException(errors);

        bh.Update(cmd.WorkDays, cmd.StartTime, cmd.EndTime, cmd.TimeZone);
        await _repo.SaveChangesAsync(ct);
    }
}
```

```csharp
// src/CRM.Application/Sla/Commands/AddHolidayCommand.cs
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record AddHolidayCommand(
    Guid BusinessHoursId, DateOnly Date, string Name) : IRequest<Guid>;

public class AddHolidayCommandHandler : IRequestHandler<AddHolidayCommand, Guid>
{
    private readonly IBusinessHoursRepository _repo;
    public AddHolidayCommandHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task<Guid> Handle(AddHolidayCommand cmd, CancellationToken ct)
    {
        var bh = await _repo.FindByIdAsync(cmd.BusinessHoursId, ct)
            ?? throw new KeyNotFoundException($"BusinessHours {cmd.BusinessHoursId} not found.");

        var holiday = bh.AddHoliday(cmd.Date, cmd.Name);
        await _repo.SaveChangesAsync(ct);
        return holiday.Id;
    }
}
```

```csharp
// src/CRM.Application/Sla/Commands/DeleteHolidayCommand.cs
using CRM.Domain.Sla;
using MediatR;

namespace CRM.Application.Sla.Commands;

public record DeleteHolidayCommand(
    Guid BusinessHoursId, Guid HolidayId) : IRequest;

public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand>
{
    private readonly IBusinessHoursRepository _repo;
    public DeleteHolidayCommandHandler(IBusinessHoursRepository repo) => _repo = repo;

    public async Task Handle(DeleteHolidayCommand cmd, CancellationToken ct)
    {
        var bh = await _repo.FindByIdAsync(cmd.BusinessHoursId, ct)
            ?? throw new KeyNotFoundException($"BusinessHours {cmd.BusinessHoursId} not found.");

        bh.RemoveHoliday(cmd.HolidayId);
        await _repo.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "BusinessHoursCrudTests" -v n
```

Expected: 7 tests PASS.

- [ ] **Step 6: Commit application layer**

```bash
git add src/CRM.Application/Sla/ \
        tests/CRM.Application.Tests/Sla/BusinessHoursCrudTests.cs
git commit -m "feat(sla): add business hours and holiday CRUD with timezone and time validation"
```

---

## Task 2: BusinessHoursController

**Files:**
- Create: `src/CRM.API/Controllers/Admin/BusinessHoursController.cs`
- Test: `tests/CRM.API.Tests/Admin/BusinessHoursControllerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Admin/BusinessHoursControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Sla.Commands;
using CRM.Application.Sla.DTOs;
using CRM.Application.Sla.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class BusinessHoursControllerTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Admin"));
        return client;
    }

    [Fact]
    public async Task GetBusinessHours_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetBusinessHoursQuery>(), default))
                 .ReturnsAsync(new List<BusinessHoursDto>
                 {
                     new(Guid.NewGuid(), null, new[] { "Monday" }, "09:00", "17:00", "UTC",
                         new List<HolidayDto>())
                 });

        var response = await BuildClient().GetAsync("/api/admin/business-hours");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBusinessHours_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UpdateBusinessHoursCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient().PutAsJsonAsync(
            $"/api/admin/business-hours/{Guid.NewGuid()}", new
            {
                workDays = new[] { "Monday", "Tuesday" },
                startTime = "09:00",
                endTime = "17:00",
                timeZone = "UTC"
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddHoliday_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<AddHolidayCommand>(), default))
                 .ReturnsAsync(Guid.NewGuid());

        var response = await BuildClient().PostAsJsonAsync(
            $"/api/admin/business-hours/{Guid.NewGuid()}/holidays", new
            {
                date = "2025-12-25",
                name = "Christmas"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteHoliday_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteHolidayCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient().DeleteAsync(
            $"/api/admin/business-hours/{Guid.NewGuid()}/holidays/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "BusinessHoursControllerTests" -v n
```

Expected: FAIL — controller does not exist.

- [ ] **Step 3: Create BusinessHoursController**

```csharp
// src/CRM.API/Controllers/Admin/BusinessHoursController.cs
using CRM.Application.Sla.Commands;
using CRM.Application.Sla.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers.Admin;

[ApiController]
[Route("api/admin/business-hours")]
[Authorize(Roles = "Admin")]
public class BusinessHoursController : ControllerBase
{
    private readonly IMediator _mediator;
    public BusinessHoursController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _mediator.Send(new GetBusinessHoursQuery(), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateBusinessHoursRequest req, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new UpdateBusinessHoursCommand(
                id, req.WorkDays,
                TimeOnly.Parse(req.StartTime),
                TimeOnly.Parse(req.EndTime),
                req.TimeZone), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/holidays")]
    public async Task<IActionResult> AddHoliday(
        Guid id, [FromBody] AddHolidayRequest req, CancellationToken ct)
    {
        try
        {
            var holidayId = await _mediator.Send(new AddHolidayCommand(
                id, DateOnly.Parse(req.Date), req.Name), ct);
            return StatusCode(201, new { id = holidayId });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}/holidays/{holidayId:guid}")]
    public async Task<IActionResult> DeleteHoliday(
        Guid id, Guid holidayId, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteHolidayCommand(id, holidayId), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record UpdateBusinessHoursRequest(
    string[] WorkDays, string StartTime, string EndTime, string TimeZone);

public record AddHolidayRequest(string Date, string Name);
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "BusinessHoursControllerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/Admin/BusinessHoursController.cs \
        tests/CRM.API.Tests/Admin/BusinessHoursControllerTests.cs
git commit -m "feat(api): add business hours CRUD endpoints with holiday management"
```
