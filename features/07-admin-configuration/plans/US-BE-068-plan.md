# CRUD Branches — Implementation Plan

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

**Story:** US-BE-068  
**Goal:** Implement `GET /api/admin/branches`, `POST /api/admin/branches`, `PUT /api/admin/branches/{id}`, `POST /api/admin/branches/{id}/deactivate`, and `POST /api/admin/branches/{id}/reactivate`. Branches are informational groupings for reporting only — no routing or SLA impact. No open-ticket guard required on deactivation (BR-ADM-014/015).

**Architecture:** `Branch` entity (Id, Name, NameAr, IsActive). `IBranchRepository`. Simple CRUD commands with no business constraints beyond unique ID. No foreign-key guards on deactivation.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Branches/Branch.cs` |
| Create | `src/CRM.Domain/Branches/IBranchRepository.cs` |
| Create | `src/CRM.Application/Admin/Branches/Commands/CreateBranchCommand.cs` |
| Create | `src/CRM.Application/Admin/Branches/Commands/UpdateBranchCommand.cs` |
| Create | `src/CRM.Application/Admin/Branches/Commands/ToggleBranchCommand.cs` |
| Create | `src/CRM.Application/Admin/Branches/Queries/ListBranchesQuery.cs` |
| Create | `src/CRM.Application/Admin/Branches/DTOs/BranchDto.cs` |
| Create | `src/CRM.API/Controllers/AdminBranchesController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/BranchCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminBranchesControllerTests.cs` |

---

## Task 1: Branch Entity + CRUD

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/BranchCommandHandlerTests.cs
using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.Queries;
using CRM.Domain.Branches;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class BranchCommandHandlerTests
{
    private readonly Mock<IBranchRepository> _repo = new();
    private readonly CreateBranchCommandHandler _createHandler;
    private readonly UpdateBranchCommandHandler _updateHandler;
    private readonly ToggleBranchCommandHandler _toggleHandler;
    private readonly ListBranchesQueryHandler _listHandler;

    public BranchCommandHandlerTests()
    {
        _createHandler = new CreateBranchCommandHandler(_repo.Object);
        _updateHandler = new UpdateBranchCommandHandler(_repo.Object);
        _toggleHandler = new ToggleBranchCommandHandler(_repo.Object);
        _listHandler = new ListBranchesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_Branch_Persists()
    {
        var result = await _createHandler.Handle(
            new CreateBranchCommand("Riyadh Branch", "فرع الرياض"), default);

        Assert.Equal("Riyadh Branch", result.Name);
        Assert.True(result.IsActive);
        _repo.Verify(r => r.AddAsync(It.IsAny<Branch>(), default), Times.Once);
    }

    [Fact]
    public async Task Update_Branch_ChangesName()
    {
        var branch = Branch.Create("Riyadh Branch", "فرع الرياض");
        _repo.Setup(r => r.FindByIdAsync(branch.Id, default)).ReturnsAsync(branch);

        var result = await _updateHandler.Handle(
            new UpdateBranchCommand(branch.Id, "Jeddah Branch", "فرع جدة"), default);

        Assert.Equal("Jeddah Branch", result.Name);
    }

    [Fact]
    public async Task Deactivate_ActiveBranch_SetsInactive()
    {
        var branch = Branch.Create("Riyadh Branch", null);
        _repo.Setup(r => r.FindByIdAsync(branch.Id, default)).ReturnsAsync(branch);

        var result = await _toggleHandler.Handle(
            new ToggleBranchCommand(branch.Id, activate: false), default);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task Reactivate_InactiveBranch_SetsActive()
    {
        var branch = Branch.Create("Riyadh Branch", null);
        branch.Deactivate();
        _repo.Setup(r => r.FindByIdAsync(branch.Id, default)).ReturnsAsync(branch);

        var result = await _toggleHandler.Handle(
            new ToggleBranchCommand(branch.Id, activate: true), default);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task List_ReturnsBranches()
    {
        _repo.Setup(r => r.ListAsync(default))
             .ReturnsAsync(new List<Branch>());

        var result = await _listHandler.Handle(new ListBranchesQuery(), default);

        Assert.Empty(result);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "BranchCommandHandlerTests" -v n
```

Expected: FAIL — `Branch` entity does not exist yet.

- [ ] **Step 3: Create Branch entity**

```csharp
// src/CRM.Domain/Branches/Branch.cs
namespace CRM.Domain.Branches;

public class Branch
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private Branch() { }

    public static Branch Create(string name, string? nameAr) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        NameAr = nameAr,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string? name, string? nameAr)
    {
        if (name is not null) Name = name;
        if (nameAr is not null) NameAr = nameAr;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
```

- [ ] **Step 4: Create IBranchRepository**

```csharp
// src/CRM.Domain/Branches/IBranchRepository.cs
namespace CRM.Domain.Branches;

public interface IBranchRepository
{
    Task<Branch?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Branch>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Branch branch, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 5: Create BranchDto**

```csharp
// src/CRM.Application/Admin/Branches/DTOs/BranchDto.cs
namespace CRM.Application.Admin.Branches.DTOs;

public record BranchDto(Guid Id, string Name, string? NameAr, bool IsActive, DateTime CreatedAt);
public record BranchActiveResult(Guid Id, bool IsActive);
```

- [ ] **Step 6: Implement commands and query**

```csharp
// src/CRM.Application/Admin/Branches/Commands/CreateBranchCommand.cs
using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Commands;

public record CreateBranchCommand(string Name, string? NameAr) : IRequest<BranchDto>;

public class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branches;
    public CreateBranchCommandHandler(IBranchRepository branches) => _branches = branches;

    public async Task<BranchDto> Handle(CreateBranchCommand cmd, CancellationToken ct)
    {
        var branch = Branch.Create(cmd.Name, cmd.NameAr);
        await _branches.AddAsync(branch, ct);
        await _branches.SaveChangesAsync(ct);
        return Map(branch);
    }

    internal static BranchDto Map(Branch b)
        => new(b.Id, b.Name, b.NameAr, b.IsActive, b.CreatedAt);
}
```

```csharp
// src/CRM.Application/Admin/Branches/Commands/UpdateBranchCommand.cs
using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Commands;

public record UpdateBranchCommand(Guid BranchId, string? Name, string? NameAr) : IRequest<BranchDto>;

public class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, BranchDto>
{
    private readonly IBranchRepository _branches;
    public UpdateBranchCommandHandler(IBranchRepository branches) => _branches = branches;

    public async Task<BranchDto> Handle(UpdateBranchCommand cmd, CancellationToken ct)
    {
        var branch = await _branches.FindByIdAsync(cmd.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {cmd.BranchId} not found.");
        branch.Update(cmd.Name, cmd.NameAr);
        await _branches.SaveChangesAsync(ct);
        return CreateBranchCommandHandler.Map(branch);
    }
}
```

```csharp
// src/CRM.Application/Admin/Branches/Commands/ToggleBranchCommand.cs
using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Commands;

public record ToggleBranchCommand(Guid BranchId, bool Activate) : IRequest<BranchActiveResult>;

public class ToggleBranchCommandHandler : IRequestHandler<ToggleBranchCommand, BranchActiveResult>
{
    private readonly IBranchRepository _branches;
    public ToggleBranchCommandHandler(IBranchRepository branches) => _branches = branches;

    public async Task<BranchActiveResult> Handle(ToggleBranchCommand cmd, CancellationToken ct)
    {
        var branch = await _branches.FindByIdAsync(cmd.BranchId, ct)
            ?? throw new KeyNotFoundException($"Branch {cmd.BranchId} not found.");
        if (cmd.Activate) branch.Reactivate(); else branch.Deactivate();
        await _branches.SaveChangesAsync(ct);
        return new BranchActiveResult(branch.Id, branch.IsActive);
    }
}
```

```csharp
// src/CRM.Application/Admin/Branches/Queries/ListBranchesQuery.cs
using CRM.Application.Admin.Branches.DTOs;
using CRM.Domain.Branches;
using MediatR;

namespace CRM.Application.Admin.Branches.Queries;

public record ListBranchesQuery : IRequest<IReadOnlyList<BranchDto>>;

public class ListBranchesQueryHandler : IRequestHandler<ListBranchesQuery, IReadOnlyList<BranchDto>>
{
    private readonly IBranchRepository _branches;
    public ListBranchesQueryHandler(IBranchRepository branches) => _branches = branches;

    public async Task<IReadOnlyList<BranchDto>> Handle(ListBranchesQuery query, CancellationToken ct)
    {
        var branches = await _branches.ListAsync(ct);
        return branches.Select(CreateBranchCommandHandler.Map).ToList();
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "BranchCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 8: Create AdminBranchesController**

```csharp
// src/CRM.API/Controllers/AdminBranchesController.cs
using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/branches")]
[Authorize(Roles = "Admin")]
public class AdminBranchesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminBranchesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(new { data = await _mediator.Send(new ListBranchesQuery(), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] BranchRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBranchCommand(req.Name, req.NameAr), ct);
        return StatusCode(201, new { data = result });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] BranchRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new UpdateBranchCommand(id, req.Name, req.NameAr), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ToggleBranchCommand(id, activate: false), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(new ToggleBranchCommand(id, activate: true), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record BranchRequest(string Name, string? NameAr);
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminBranchesControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminBranchesControllerTests
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
    public async Task Create_Branch_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateBranchCommand>(), default))
                 .ReturnsAsync(new BranchDto(
                     Guid.NewGuid(), "Riyadh Branch", null, true, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/branches", new { name = "Riyadh Branch" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ToggleBranchCommand>(), default))
                 .ReturnsAsync(new BranchActiveResult(Guid.NewGuid(), false));

        var response = await BuildClient()
            .PostAsync($"/api/admin/branches/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminBranchesControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Domain/Branches/ \
        src/CRM.Application/Admin/Branches/ \
        src/CRM.API/Controllers/AdminBranchesController.cs \
        tests/CRM.Application.Tests/Admin/BranchCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminBranchesControllerTests.cs
git commit -m "feat(admin): add Branch CRUD — GET/POST/PUT + deactivate/reactivate (reporting entity, no routing impact)"
```
