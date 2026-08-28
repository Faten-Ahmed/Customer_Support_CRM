# CRUD Departments — Implementation Plan

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

**Story:** US-BE-067  
**Goal:** Implement `GET /api/admin/departments`, `POST /api/admin/departments`, `PUT /api/admin/departments/{id}`, `POST /api/admin/departments/{id}/deactivate`, and `POST /api/admin/departments/{id}/reactivate`. Unique name constraint (409 on duplicate). Deactivation blocked if open tickets exist (422).

**Architecture:** `Department` entity with `IsActive` flag. `IDepartmentRepository`. `CreateDepartmentCommand` checks name uniqueness. `DeactivateDepartmentCommand` checks open ticket count. `GET` is readable by Admin and Manager.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Departments/Department.cs` |
| Create | `src/CRM.Domain/Departments/IDepartmentRepository.cs` |
| Create | `src/CRM.Application/Admin/Departments/Commands/CreateDepartmentCommand.cs` |
| Create | `src/CRM.Application/Admin/Departments/Commands/UpdateDepartmentCommand.cs` |
| Create | `src/CRM.Application/Admin/Departments/Commands/DeactivateDepartmentCommand.cs` |
| Create | `src/CRM.Application/Admin/Departments/Queries/ListDepartmentsQuery.cs` |
| Create | `src/CRM.Application/Admin/Departments/DTOs/DepartmentDto.cs` |
| Create | `src/CRM.API/Controllers/AdminDepartmentsController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/DepartmentCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminDepartmentsControllerTests.cs` |

---

## Task 1: Department Entity + CRUD Commands

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/DepartmentCommandHandlerTests.cs
using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.Queries;
using CRM.Application.Common;
using CRM.Domain.Departments;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class DepartmentCommandHandlerTests
{
    private readonly Mock<IDepartmentRepository> _repo = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly CreateDepartmentCommandHandler _createHandler;
    private readonly UpdateDepartmentCommandHandler _updateHandler;
    private readonly DeactivateDepartmentCommandHandler _deactivateHandler;
    private readonly ListDepartmentsQueryHandler _listHandler;

    public DepartmentCommandHandlerTests()
    {
        _createHandler = new CreateDepartmentCommandHandler(_repo.Object);
        _updateHandler = new UpdateDepartmentCommandHandler(_repo.Object);
        _deactivateHandler = new DeactivateDepartmentCommandHandler(_repo.Object, _tickets.Object);
        _listHandler = new ListDepartmentsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_UniqueName_Succeeds()
    {
        _repo.Setup(r => r.ExistsByNameAsync("Technical Support", default)).ReturnsAsync(false);

        var result = await _createHandler.Handle(
            new CreateDepartmentCommand("Technical Support", "الدعم الفني", null, null),
            default);

        Assert.Equal("Technical Support", result.Name);
        _repo.Verify(r => r.AddAsync(It.IsAny<Department>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsInvalidOperationExceptionWith409()
    {
        _repo.Setup(r => r.ExistsByNameAsync("Technical Support", default)).ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateDepartmentCommand("Technical Support", null, null, null),
                default));

        Assert.Contains("409", ex.Message);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_ThrowsInvalidOperationException()
    {
        var dept = Department.Create("Technical Support", null, null, null);
        _repo.Setup(r => r.FindByIdAsync(dept.Id, default)).ReturnsAsync(dept);
        _tickets.Setup(t => t.CountOpenForDepartmentAsync(dept.Id, default)).ReturnsAsync(3);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateDepartmentCommand(dept.Id), default));

        Assert.Contains("3", ex.Message);
    }

    [Fact]
    public async Task Deactivate_NoOpenTickets_Succeeds()
    {
        var dept = Department.Create("Technical Support", null, null, null);
        _repo.Setup(r => r.FindByIdAsync(dept.Id, default)).ReturnsAsync(dept);
        _tickets.Setup(t => t.CountOpenForDepartmentAsync(dept.Id, default)).ReturnsAsync(0);

        var result = await _deactivateHandler.Handle(
            new DeactivateDepartmentCommand(dept.Id), default);

        Assert.False(result.IsActive);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DepartmentCommandHandlerTests" -v n
```

Expected: FAIL — `Department` entity does not exist yet.

- [ ] **Step 3: Create Department entity**

```csharp
// src/CRM.Domain/Departments/Department.cs
namespace CRM.Domain.Departments;

public class Department
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public string? Description { get; private set; }
    public Guid? BusinessHoursId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private Department() { }

    public static Department Create(
        string name, string? nameAr, string? description, Guid? businessHoursId)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameAr = nameAr,
            Description = description,
            BusinessHoursId = businessHoursId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(string? name, string? nameAr, string? description, Guid? businessHoursId)
    {
        if (name is not null) Name = name;
        if (nameAr is not null) NameAr = nameAr;
        if (description is not null) Description = description;
        if (businessHoursId.HasValue) BusinessHoursId = businessHoursId;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
```

- [ ] **Step 4: Create IDepartmentRepository and add CountOpenForDepartmentAsync to ITicketRepository**

```csharp
// src/CRM.Domain/Departments/IDepartmentRepository.cs
namespace CRM.Domain.Departments;

public interface IDepartmentRepository
{
    Task<Department?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Department dept, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

Add to `src/CRM.Domain/Tickets/ITicketRepository.cs`:
```csharp
Task<int> CountOpenForDepartmentAsync(Guid departmentId, CancellationToken ct = default);
```

- [ ] **Step 5: Create DepartmentDto**

```csharp
// src/CRM.Application/Admin/Departments/DTOs/DepartmentDto.cs
namespace CRM.Application.Admin.Departments.DTOs;

public record DepartmentDto(
    Guid Id, string Name, string? NameAr, string? Description,
    Guid? BusinessHoursId, bool IsActive, DateTime CreatedAt);

public record DepartmentActiveResult(Guid Id, bool IsActive);
```

- [ ] **Step 6: Implement commands and query**

```csharp
// src/CRM.Application/Admin/Departments/Commands/CreateDepartmentCommand.cs
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record CreateDepartmentCommand(
    string Name, string? NameAr, string? Description, Guid? BusinessHoursId)
    : IRequest<DepartmentDto>;

public class CreateDepartmentCommandHandler
    : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IDepartmentRepository _depts;

    public CreateDepartmentCommandHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task<DepartmentDto> Handle(
        CreateDepartmentCommand cmd, CancellationToken ct)
    {
        bool exists = await _depts.ExistsByNameAsync(cmd.Name, ct);
        if (exists)
            throw new InvalidOperationException(
                $"409: A department named '{cmd.Name}' already exists.");

        var dept = Department.Create(cmd.Name, cmd.NameAr, cmd.Description, cmd.BusinessHoursId);
        await _depts.AddAsync(dept, ct);
        await _depts.SaveChangesAsync(ct);

        return Map(dept);
    }

    internal static DepartmentDto Map(Department d)
        => new(d.Id, d.Name, d.NameAr, d.Description, d.BusinessHoursId, d.IsActive, d.CreatedAt);
}
```

```csharp
// src/CRM.Application/Admin/Departments/Commands/UpdateDepartmentCommand.cs
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record UpdateDepartmentCommand(
    Guid DeptId, string? Name, string? NameAr, string? Description, Guid? BusinessHoursId)
    : IRequest<DepartmentDto>;

public class UpdateDepartmentCommandHandler
    : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    private readonly IDepartmentRepository _depts;

    public UpdateDepartmentCommandHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(cmd.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {cmd.DeptId} not found.");

        dept.Update(cmd.Name, cmd.NameAr, cmd.Description, cmd.BusinessHoursId);
        await _depts.SaveChangesAsync(ct);

        return CreateDepartmentCommandHandler.Map(dept);
    }
}
```

```csharp
// src/CRM.Application/Admin/Departments/Commands/DeactivateDepartmentCommand.cs
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record DeactivateDepartmentCommand(Guid DeptId) : IRequest<DepartmentActiveResult>;

public class DeactivateDepartmentCommandHandler
    : IRequestHandler<DeactivateDepartmentCommand, DepartmentActiveResult>
{
    private readonly IDepartmentRepository _depts;
    private readonly ITicketRepository _tickets;

    public DeactivateDepartmentCommandHandler(
        IDepartmentRepository depts, ITicketRepository tickets)
    {
        _depts = depts;
        _tickets = tickets;
    }

    public async Task<DepartmentActiveResult> Handle(
        DeactivateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(cmd.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {cmd.DeptId} not found.");

        int openTickets = await _tickets.CountOpenForDepartmentAsync(cmd.DeptId, ct);
        if (openTickets > 0)
            throw new InvalidOperationException(
                $"Cannot deactivate department with {openTickets} open ticket(s). Resolve or reassign them first.");

        dept.Deactivate();
        await _depts.SaveChangesAsync(ct);

        return new DepartmentActiveResult(dept.Id, dept.IsActive);
    }
}
```

```csharp
// src/CRM.Application/Admin/Departments/Queries/ListDepartmentsQuery.cs
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Queries;

public record ListDepartmentsQuery : IRequest<IReadOnlyList<DepartmentDto>>;

public class ListDepartmentsQueryHandler
    : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _depts;

    public ListDepartmentsQueryHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        ListDepartmentsQuery query, CancellationToken ct)
    {
        var depts = await _depts.ListAsync(ct);
        return depts.Select(CreateDepartmentCommandHandler.Map).ToList();
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DepartmentCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Create AdminDepartmentsController**

```csharp
// src/CRM.API/Controllers/AdminDepartmentsController.cs
using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/departments")]
[Authorize(Roles = "Admin")]
public class AdminDepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminDepartmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new ListDepartmentsQuery(), ct);
        return Ok(new { data = result });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeptRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateDepartmentCommand(req.Name, req.NameAr, req.Description, req.BusinessHoursId), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("409"))
            { return Conflict(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateDeptRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateDepartmentCommand(id, req.Name, req.NameAr, req.Description, req.BusinessHoursId), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new DeactivateDepartmentCommand(id), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var dept = await _mediator.Send(new GetDepartmentQuery(id), ct);
            // Simple reactivation — no constraints
            await _mediator.Send(new ReactivateDepartmentCommand(id), ct);
            return Ok(new { data = new { id, isActive = true } });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record CreateDeptRequest(string Name, string? NameAr, string? Description, Guid? BusinessHoursId);
public record UpdateDeptRequest(string? Name, string? NameAr, string? Description, Guid? BusinessHoursId);
```

Add `ReactivateDepartmentCommand` to `src/CRM.Application/Admin/Departments/Commands/`:

```csharp
// src/CRM.Application/Admin/Departments/Commands/ReactivateDepartmentCommand.cs
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Commands;

public record ReactivateDepartmentCommand(Guid DeptId) : IRequest;

public class ReactivateDepartmentCommandHandler : IRequestHandler<ReactivateDepartmentCommand>
{
    private readonly IDepartmentRepository _depts;

    public ReactivateDepartmentCommandHandler(IDepartmentRepository depts) => _depts = depts;

    public async Task Handle(ReactivateDepartmentCommand cmd, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(cmd.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {cmd.DeptId} not found.");
        dept.Reactivate();
        await _depts.SaveChangesAsync(ct);
    }
}
```

Add `GetDepartmentQuery`:

```csharp
// src/CRM.Application/Admin/Departments/Queries/GetDepartmentQuery.cs
using CRM.Application.Admin.Departments.DTOs;
using CRM.Domain.Departments;
using MediatR;

namespace CRM.Application.Admin.Departments.Queries;

public record GetDepartmentQuery(Guid DeptId) : IRequest<DepartmentDto>;

public class GetDepartmentQueryHandler : IRequestHandler<GetDepartmentQuery, DepartmentDto>
{
    private readonly IDepartmentRepository _depts;
    public GetDepartmentQueryHandler(IDepartmentRepository depts) => _depts = depts;
    public async Task<DepartmentDto> Handle(GetDepartmentQuery query, CancellationToken ct)
    {
        var dept = await _depts.FindByIdAsync(query.DeptId, ct)
            ?? throw new KeyNotFoundException($"Department {query.DeptId} not found.");
        return CreateDepartmentCommandHandler.Map(dept);
    }
}
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminDepartmentsControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminDepartmentsControllerTests
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
    public async Task Create_UniqueName_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateDepartmentCommand>(), default))
                 .ReturnsAsync(new DepartmentDto(
                     Guid.NewGuid(), "Technical Support", null, null, null, true, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/departments",
            new { name = "Technical Support" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateDepartmentCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("409: Department exists."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/departments",
            new { name = "Technical Support" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateDepartmentCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Cannot deactivate: 3 open tickets."));

        var response = await BuildClient()
            .PostAsync($"/api/admin/departments/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminDepartmentsControllerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Domain/Departments/ \
        src/CRM.Application/Admin/Departments/ \
        src/CRM.API/Controllers/AdminDepartmentsController.cs \
        tests/CRM.Application.Tests/Admin/DepartmentCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminDepartmentsControllerTests.cs
git commit -m "feat(admin): add Department CRUD — GET/POST/PUT + deactivate/reactivate with open-ticket guard"
```
