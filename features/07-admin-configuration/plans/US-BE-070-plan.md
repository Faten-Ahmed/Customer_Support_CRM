# CRUD Ticket Field Definitions — Implementation Plan

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

**Story:** US-BE-070  
**Goal:** Implement `GET /api/admin/field-definitions`, `POST /api/admin/field-definitions`, `PUT /api/admin/field-definitions/{id}`, and `DELETE /api/admin/field-definitions/{id}` (soft-deactivate). Field types: Text, Number, Date, Dropdown, Checkbox. Dropdown requires 2–20 options. The `TicketFieldDefinition` entity was introduced in US-BE-038 — this plan adds the admin CRUD layer on top.

**Architecture:** `TicketFieldDefinition` entity (already defined in US-BE-038 as part of `CustomFieldValidator`). `ITicketFieldDefinitionRepository` (already defined). Add `CreateFieldDefinitionCommand`, `UpdateFieldDefinitionCommand`, `DeactivateFieldDefinitionCommand` in the admin application layer.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Admin/FieldDefinitions/Commands/CreateFieldDefinitionCommand.cs` |
| Create | `src/CRM.Application/Admin/FieldDefinitions/Commands/UpdateFieldDefinitionCommand.cs` |
| Create | `src/CRM.Application/Admin/FieldDefinitions/Commands/DeactivateFieldDefinitionCommand.cs` |
| Create | `src/CRM.Application/Admin/FieldDefinitions/Queries/ListFieldDefinitionsQuery.cs` |
| Create | `src/CRM.Application/Admin/FieldDefinitions/DTOs/FieldDefinitionDto.cs` |
| Create | `src/CRM.API/Controllers/AdminFieldDefinitionsController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/FieldDefinitionCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminFieldDefinitionsControllerTests.cs` |

---

## Task 1: Field Definition CRUD Commands

> Note: `TicketFieldDefinition` entity and `ITicketFieldDefinitionRepository` are defined in US-BE-038-plan. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/FieldDefinitionCommandHandlerTests.cs
using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.Queries;
using CRM.Domain.Tickets;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class FieldDefinitionCommandHandlerTests
{
    private readonly Mock<ITicketFieldDefinitionRepository> _repo = new();
    private readonly CreateFieldDefinitionCommandHandler _createHandler;
    private readonly UpdateFieldDefinitionCommandHandler _updateHandler;
    private readonly DeactivateFieldDefinitionCommandHandler _deactivateHandler;
    private readonly ListFieldDefinitionsQueryHandler _listHandler;

    public FieldDefinitionCommandHandlerTests()
    {
        _createHandler = new CreateFieldDefinitionCommandHandler(_repo.Object);
        _updateHandler = new UpdateFieldDefinitionCommandHandler(_repo.Object);
        _deactivateHandler = new DeactivateFieldDefinitionCommandHandler(_repo.Object);
        _listHandler = new ListFieldDefinitionsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_TextField_Succeeds()
    {
        var deptId = Guid.NewGuid();

        var result = await _createHandler.Handle(
            new CreateFieldDefinitionCommand(
                deptId, null, "Serial Number", "الرقم التسلسلي",
                FieldType.Text, null, false, 1),
            default);

        Assert.Equal("Serial Number", result.FieldName);
        Assert.True(result.IsActive);
        _repo.Verify(r => r.AddAsync(It.IsAny<TicketFieldDefinition>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_DropdownWithOneOption_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateFieldDefinitionCommand(
                    Guid.NewGuid(), null, "Status", null,
                    FieldType.Dropdown, new[] { "OnlyOption" }, false, 1),
                default));
    }

    [Fact]
    public async Task Create_DropdownWith21Options_ThrowsInvalidOperationException()
    {
        var options = Enumerable.Range(1, 21).Select(i => $"Option {i}").ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateFieldDefinitionCommand(
                    Guid.NewGuid(), null, "Status", null,
                    FieldType.Dropdown, options, false, 1),
                default));
    }

    [Fact]
    public async Task Deactivate_SetsIsActiveFalse()
    {
        var field = TicketFieldDefinition.Create(
            Guid.NewGuid(), null, "Serial Number", null, FieldType.Text, null, false, 1);
        _repo.Setup(r => r.FindByIdAsync(field.Id, default)).ReturnsAsync(field);

        await _deactivateHandler.Handle(
            new DeactivateFieldDefinitionCommand(field.Id), default);

        Assert.False(field.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "FieldDefinitionCommandHandlerTests" -v n
```

Expected: FAIL — `CreateFieldDefinitionCommand` does not exist yet.

- [ ] **Step 3: Create FieldDefinitionDto**

```csharp
// src/CRM.Application/Admin/FieldDefinitions/DTOs/FieldDefinitionDto.cs
namespace CRM.Application.Admin.FieldDefinitions.DTOs;

public record FieldDefinitionDto(
    Guid Id,
    Guid DepartmentId,
    Guid? CategoryId,
    string FieldName,
    string? FieldNameAr,
    string FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int SortOrder,
    bool IsActive);
```

- [ ] **Step 4: Implement CreateFieldDefinitionCommand**

```csharp
// src/CRM.Application/Admin/FieldDefinitions/Commands/CreateFieldDefinitionCommand.cs
using CRM.Application.Admin.FieldDefinitions.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Commands;

public record CreateFieldDefinitionCommand(
    Guid DepartmentId,
    Guid? CategoryId,
    string FieldName,
    string? FieldNameAr,
    FieldType FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int SortOrder) : IRequest<FieldDefinitionDto>;

public class CreateFieldDefinitionCommandHandler
    : IRequestHandler<CreateFieldDefinitionCommand, FieldDefinitionDto>
{
    private readonly ITicketFieldDefinitionRepository _repo;

    public CreateFieldDefinitionCommandHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        CreateFieldDefinitionCommand cmd, CancellationToken ct)
    {
        if (cmd.FieldType == FieldType.Dropdown)
        {
            int count = cmd.Options?.Count ?? 0;
            if (count < 2 || count > 20)
                throw new InvalidOperationException(
                    "Dropdown field must have between 2 and 20 options.");
        }

        var field = TicketFieldDefinition.Create(
            cmd.DepartmentId, cmd.CategoryId,
            cmd.FieldName, cmd.FieldNameAr,
            cmd.FieldType, cmd.Options, cmd.IsRequired, cmd.SortOrder);

        await _repo.AddAsync(field, ct);
        await _repo.SaveChangesAsync(ct);

        return Map(field);
    }

    internal static FieldDefinitionDto Map(TicketFieldDefinition f)
        => new(f.Id, f.DepartmentId, f.CategoryId, f.FieldName, f.FieldNameAr,
               f.FieldType.ToString(), f.Options, f.IsRequired, f.SortOrder, f.IsActive);
}
```

- [ ] **Step 5: Implement UpdateFieldDefinitionCommand**

```csharp
// src/CRM.Application/Admin/FieldDefinitions/Commands/UpdateFieldDefinitionCommand.cs
using CRM.Application.Admin.FieldDefinitions.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Commands;

public record UpdateFieldDefinitionCommand(
    Guid FieldId,
    string? FieldName,
    string? FieldNameAr,
    IReadOnlyList<string>? Options,
    bool? IsRequired,
    int? SortOrder) : IRequest<FieldDefinitionDto>;

public class UpdateFieldDefinitionCommandHandler
    : IRequestHandler<UpdateFieldDefinitionCommand, FieldDefinitionDto>
{
    private readonly ITicketFieldDefinitionRepository _repo;
    public UpdateFieldDefinitionCommandHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        UpdateFieldDefinitionCommand cmd, CancellationToken ct)
    {
        var field = await _repo.FindByIdAsync(cmd.FieldId, ct)
            ?? throw new KeyNotFoundException($"Field definition {cmd.FieldId} not found.");

        if (cmd.Options is not null && field.FieldType == FieldType.Dropdown)
        {
            if (cmd.Options.Count < 2 || cmd.Options.Count > 20)
                throw new InvalidOperationException(
                    "Dropdown field must have between 2 and 20 options.");
        }

        field.Update(cmd.FieldName, cmd.FieldNameAr, cmd.Options, cmd.IsRequired, cmd.SortOrder);
        await _repo.SaveChangesAsync(ct);

        return CreateFieldDefinitionCommandHandler.Map(field);
    }
}
```

- [ ] **Step 6: Implement DeactivateFieldDefinitionCommand**

```csharp
// src/CRM.Application/Admin/FieldDefinitions/Commands/DeactivateFieldDefinitionCommand.cs
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Commands;

public record DeactivateFieldDefinitionCommand(Guid FieldId) : IRequest;

public class DeactivateFieldDefinitionCommandHandler
    : IRequestHandler<DeactivateFieldDefinitionCommand>
{
    private readonly ITicketFieldDefinitionRepository _repo;
    public DeactivateFieldDefinitionCommandHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task Handle(DeactivateFieldDefinitionCommand cmd, CancellationToken ct)
    {
        var field = await _repo.FindByIdAsync(cmd.FieldId, ct)
            ?? throw new KeyNotFoundException($"Field definition {cmd.FieldId} not found.");
        field.Deactivate();
        await _repo.SaveChangesAsync(ct);
    }
}
```

Ensure `TicketFieldDefinition` (from US-BE-038) has these methods — add if missing:
```csharp
public bool IsActive { get; private set; } = true;

public static TicketFieldDefinition Create(
    Guid departmentId, Guid? categoryId, string fieldName, string? fieldNameAr,
    FieldType fieldType, IReadOnlyList<string>? options, bool isRequired, int sortOrder)
    => new() { /* ... all fields ... */ };

public void Update(string? fieldName, string? fieldNameAr,
    IReadOnlyList<string>? options, bool? isRequired, int? sortOrder)
{
    if (fieldName is not null) FieldName = fieldName;
    if (fieldNameAr is not null) FieldNameAr = fieldNameAr;
    if (options is not null) Options = options;
    if (isRequired.HasValue) IsRequired = isRequired.Value;
    if (sortOrder.HasValue) SortOrder = sortOrder.Value;
}

public void Deactivate() => IsActive = false;
```

- [ ] **Step 7: Implement ListFieldDefinitionsQuery**

```csharp
// src/CRM.Application/Admin/FieldDefinitions/Queries/ListFieldDefinitionsQuery.cs
using CRM.Application.Admin.FieldDefinitions.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.FieldDefinitions.Queries;

public record ListFieldDefinitionsQuery(
    Guid? DepartmentId, Guid? CategoryId) : IRequest<IReadOnlyList<FieldDefinitionDto>>;

public class ListFieldDefinitionsQueryHandler
    : IRequestHandler<ListFieldDefinitionsQuery, IReadOnlyList<FieldDefinitionDto>>
{
    private readonly ITicketFieldDefinitionRepository _repo;
    public ListFieldDefinitionsQueryHandler(ITicketFieldDefinitionRepository repo)
        => _repo = repo;

    public async Task<IReadOnlyList<FieldDefinitionDto>> Handle(
        ListFieldDefinitionsQuery query, CancellationToken ct)
    {
        var fields = await _repo.GetActiveAsync(query.DepartmentId, query.CategoryId, ct);
        return fields.Select(CreateFieldDefinitionCommandHandler.Map).ToList();
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "FieldDefinitionCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 9: Create AdminFieldDefinitionsController**

```csharp
// src/CRM.API/Controllers/AdminFieldDefinitionsController.cs
using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.Queries;
using CRM.Domain.Tickets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/field-definitions")]
[Authorize(Roles = "Admin")]
public class AdminFieldDefinitionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminFieldDefinitionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? categoryId,
        CancellationToken ct)
        => Ok(new { data = await _mediator.Send(
            new ListFieldDefinitionsQuery(departmentId, categoryId), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] FieldDefinitionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<FieldType>(req.FieldType, out var fieldType))
            return BadRequest(new { error = "Invalid field type." });

        try
        {
            var result = await _mediator.Send(
                new CreateFieldDefinitionCommand(
                    req.DepartmentId, req.CategoryId, req.FieldName, req.FieldNameAr,
                    fieldType, req.Options, req.IsRequired, req.SortOrder), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateFieldDefinitionRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateFieldDefinitionCommand(
                    id, req.FieldName, req.FieldNameAr, req.Options, req.IsRequired, req.SortOrder), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeactivateFieldDefinitionCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record FieldDefinitionRequest(
    Guid DepartmentId, Guid? CategoryId,
    string FieldName, string? FieldNameAr,
    string FieldType, IReadOnlyList<string>? Options,
    bool IsRequired, int SortOrder);

public record UpdateFieldDefinitionRequest(
    string? FieldName, string? FieldNameAr,
    IReadOnlyList<string>? Options, bool? IsRequired, int? SortOrder);
```

- [ ] **Step 10: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminFieldDefinitionsControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.FieldDefinitions.Commands;
using CRM.Application.Admin.FieldDefinitions.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminFieldDefinitionsControllerTests
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
    public async Task Create_TextField_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateFieldDefinitionCommand>(), default))
                 .ReturnsAsync(new FieldDefinitionDto(
                     Guid.NewGuid(), Guid.NewGuid(), null, "Serial Number", null,
                     "Text", null, false, 1, true));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/field-definitions",
            new
            {
                departmentId = Guid.NewGuid(),
                fieldName = "Serial Number",
                fieldType = "Text",
                isRequired = false,
                sortOrder = 1
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_DropdownWithOneOption_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateFieldDefinitionCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException(
                     "Dropdown field must have between 2 and 20 options."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/field-definitions",
            new
            {
                departmentId = Guid.NewGuid(),
                fieldName = "Status",
                fieldType = "Dropdown",
                options = new[] { "OnlyOption" },
                isRequired = false,
                sortOrder = 1
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeactivates_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateFieldDefinitionCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient()
            .DeleteAsync($"/api/admin/field-definitions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
```

- [ ] **Step 11: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminFieldDefinitionsControllerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 12: Commit**

```bash
git add src/CRM.Application/Admin/FieldDefinitions/ \
        src/CRM.API/Controllers/AdminFieldDefinitionsController.cs \
        tests/CRM.Application.Tests/Admin/FieldDefinitionCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminFieldDefinitionsControllerTests.cs
git commit -m "feat(admin): add Field Definition CRUD — GET/POST/PUT + soft-delete with Dropdown 2-20 options constraint"
```
