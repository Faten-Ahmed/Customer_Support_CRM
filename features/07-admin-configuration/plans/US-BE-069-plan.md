# CRUD Ticket Categories — Implementation Plan

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

**Story:** US-BE-069  
**Goal:** Implement `GET /api/admin/categories` (tree), `POST /api/admin/categories`, `PUT /api/admin/categories/{id}`, `POST /api/admin/categories/{id}/deactivate`, and `POST /api/admin/categories/{id}/reactivate`. Two-level tree only (children cannot have children). Deactivating a parent deactivates all children. Blocked by open tickets (422).

**Architecture:** `TicketCategory` entity with optional `ParentCategoryId`. `ICategoryRepository`. `DeactivateCategoryCommand` cascades to children in one transaction. `GET` returns flat list that the API shapes into a tree.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Categories/TicketCategory.cs` |
| Create | `src/CRM.Domain/Categories/ICategoryRepository.cs` |
| Create | `src/CRM.Application/Admin/Categories/Commands/CreateCategoryCommand.cs` |
| Create | `src/CRM.Application/Admin/Categories/Commands/UpdateCategoryCommand.cs` |
| Create | `src/CRM.Application/Admin/Categories/Commands/DeactivateCategoryCommand.cs` |
| Create | `src/CRM.Application/Admin/Categories/Commands/ReactivateCategoryCommand.cs` |
| Create | `src/CRM.Application/Admin/Categories/Queries/ListCategoriesQuery.cs` |
| Create | `src/CRM.Application/Admin/Categories/DTOs/CategoryDto.cs` |
| Create | `src/CRM.API/Controllers/AdminCategoriesController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/CategoryCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminCategoriesControllerTests.cs` |

---

## Task 1: TicketCategory Entity + CRUD

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/CategoryCommandHandlerTests.cs
using CRM.Application.Admin.Categories.Commands;
using CRM.Application.Admin.Categories.Queries;
using CRM.Domain.Categories;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class CategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _repo = new();
    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly CreateCategoryCommandHandler _createHandler;
    private readonly DeactivateCategoryCommandHandler _deactivateHandler;
    private readonly ListCategoriesQueryHandler _listHandler;

    public CategoryCommandHandlerTests()
    {
        _createHandler = new CreateCategoryCommandHandler(_repo.Object);
        _deactivateHandler = new DeactivateCategoryCommandHandler(_repo.Object, _tickets.Object);
        _listHandler = new ListCategoriesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_ParentCategory_Succeeds()
    {
        var result = await _createHandler.Handle(
            new CreateCategoryCommand("Technical Support", "الدعم الفني", null, 1),
            default);

        Assert.Equal("Technical Support", result.Name);
        Assert.Null(result.ParentCategoryId);
        _repo.Verify(r => r.AddAsync(It.IsAny<TicketCategory>(), default), Times.Once);
    }

    [Fact]
    public async Task Create_ChildOfChild_ThrowsInvalidOperationException()
    {
        var grandchild = Guid.NewGuid(); // represents an id of a category that is already a child
        _repo.Setup(r => r.IsChildCategoryAsync(grandchild, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _createHandler.Handle(
                new CreateCategoryCommand("Grandchild", null, grandchild, 1),
                default));
    }

    [Fact]
    public async Task Deactivate_Parent_CascadesToChildren()
    {
        var parent = TicketCategory.Create("Technical Support", null, null, 1);
        var child = TicketCategory.Create("Hardware", null, parent.Id, 1);
        _repo.Setup(r => r.FindByIdAsync(parent.Id, default)).ReturnsAsync(parent);
        _repo.Setup(r => r.GetChildrenAsync(parent.Id, default))
             .ReturnsAsync(new List<TicketCategory> { child });
        _tickets.Setup(t => t.CountOpenForCategoryAsync(parent.Id, default)).ReturnsAsync(0);
        _tickets.Setup(t => t.CountOpenForCategoryAsync(child.Id, default)).ReturnsAsync(0);

        await _deactivateHandler.Handle(
            new DeactivateCategoryCommand(parent.Id), default);

        Assert.False(parent.IsActive);
        Assert.False(child.IsActive);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_ThrowsInvalidOperationException()
    {
        var category = TicketCategory.Create("Technical Support", null, null, 1);
        _repo.Setup(r => r.FindByIdAsync(category.Id, default)).ReturnsAsync(category);
        _repo.Setup(r => r.GetChildrenAsync(category.Id, default))
             .ReturnsAsync(new List<TicketCategory>());
        _tickets.Setup(t => t.CountOpenForCategoryAsync(category.Id, default)).ReturnsAsync(5);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deactivateHandler.Handle(
                new DeactivateCategoryCommand(category.Id), default));

        Assert.Contains("5", ex.Message);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CategoryCommandHandlerTests" -v n
```

Expected: FAIL — `TicketCategory` does not exist yet.

- [ ] **Step 3: Create TicketCategory entity**

```csharp
// src/CRM.Domain/Categories/TicketCategory.cs
namespace CRM.Domain.Categories;

public class TicketCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private TicketCategory() { }

    public static TicketCategory Create(
        string name, string? nameAr, Guid? parentCategoryId, int sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameAr = nameAr,
            ParentCategoryId = parentCategoryId,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(string? name, string? nameAr, int? sortOrder)
    {
        if (name is not null) Name = name;
        if (nameAr is not null) NameAr = nameAr;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
```

- [ ] **Step 4: Create ICategoryRepository and add CountOpenForCategoryAsync**

```csharp
// src/CRM.Domain/Categories/ICategoryRepository.cs
namespace CRM.Domain.Categories;

public interface ICategoryRepository
{
    Task<TicketCategory?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TicketCategory>> ListAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TicketCategory>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);
    Task<bool> IsChildCategoryAsync(Guid categoryId, CancellationToken ct = default);
    Task AddAsync(TicketCategory category, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

Add to `src/CRM.Domain/Tickets/ITicketRepository.cs`:
```csharp
Task<int> CountOpenForCategoryAsync(Guid categoryId, CancellationToken ct = default);
```

- [ ] **Step 5: Create CategoryDto**

```csharp
// src/CRM.Application/Admin/Categories/DTOs/CategoryDto.cs
namespace CRM.Application.Admin.Categories.DTOs;

public record CategoryDto(
    Guid Id, string Name, string? NameAr,
    Guid? ParentCategoryId, int SortOrder, bool IsActive,
    IReadOnlyList<CategoryDto>? Children = null);
```

- [ ] **Step 6: Implement commands**

```csharp
// src/CRM.Application/Admin/Categories/Commands/CreateCategoryCommand.cs
using CRM.Application.Admin.Categories.DTOs;
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record CreateCategoryCommand(
    string Name, string? NameAr, Guid? ParentCategoryId, int SortOrder)
    : IRequest<CategoryDto>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categories;
    public CreateCategoryCommandHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<CategoryDto> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        if (cmd.ParentCategoryId.HasValue)
        {
            bool parentIsChild = await _categories.IsChildCategoryAsync(
                cmd.ParentCategoryId.Value, ct);
            if (parentIsChild)
                throw new InvalidOperationException(
                    "Maximum category depth is 1. A child category cannot have children.");
        }

        var category = TicketCategory.Create(
            cmd.Name, cmd.NameAr, cmd.ParentCategoryId, cmd.SortOrder);
        await _categories.AddAsync(category, ct);
        await _categories.SaveChangesAsync(ct);

        return Map(category);
    }

    internal static CategoryDto Map(TicketCategory c, IReadOnlyList<CategoryDto>? children = null)
        => new(c.Id, c.Name, c.NameAr, c.ParentCategoryId, c.SortOrder, c.IsActive, children);
}
```

```csharp
// src/CRM.Application/Admin/Categories/Commands/DeactivateCategoryCommand.cs
using CRM.Domain.Categories;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record DeactivateCategoryCommand(Guid CategoryId) : IRequest;

public class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand>
{
    private readonly ICategoryRepository _categories;
    private readonly ITicketRepository _tickets;

    public DeactivateCategoryCommandHandler(
        ICategoryRepository categories, ITicketRepository tickets)
    {
        _categories = categories;
        _tickets = tickets;
    }

    public async Task Handle(DeactivateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found.");

        int openTickets = await _tickets.CountOpenForCategoryAsync(cmd.CategoryId, ct);
        if (openTickets > 0)
            throw new InvalidOperationException(
                $"Cannot deactivate: {openTickets} open ticket(s) assigned to this category.");

        var children = await _categories.GetChildrenAsync(cmd.CategoryId, ct);
        foreach (var child in children)
            child.Deactivate();

        category.Deactivate();
        await _categories.SaveChangesAsync(ct);
    }
}
```

```csharp
// src/CRM.Application/Admin/Categories/Commands/UpdateCategoryCommand.cs
using CRM.Application.Admin.Categories.DTOs;
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record UpdateCategoryCommand(
    Guid CategoryId, string? Name, string? NameAr, int? SortOrder)
    : IRequest<CategoryDto>;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categories;
    public UpdateCategoryCommandHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<CategoryDto> Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found.");
        category.Update(cmd.Name, cmd.NameAr, cmd.SortOrder);
        await _categories.SaveChangesAsync(ct);
        return CreateCategoryCommandHandler.Map(category);
    }
}
```

```csharp
// src/CRM.Application/Admin/Categories/Commands/ReactivateCategoryCommand.cs
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Commands;

public record ReactivateCategoryCommand(Guid CategoryId) : IRequest;

public class ReactivateCategoryCommandHandler : IRequestHandler<ReactivateCategoryCommand>
{
    private readonly ICategoryRepository _categories;
    public ReactivateCategoryCommandHandler(ICategoryRepository categories) => _categories = categories;

    public async Task Handle(ReactivateCategoryCommand cmd, CancellationToken ct)
    {
        var category = await _categories.FindByIdAsync(cmd.CategoryId, ct)
            ?? throw new KeyNotFoundException($"Category {cmd.CategoryId} not found.");
        category.Reactivate();
        await _categories.SaveChangesAsync(ct);
    }
}
```

```csharp
// src/CRM.Application/Admin/Categories/Queries/ListCategoriesQuery.cs
using CRM.Application.Admin.Categories.DTOs;
using CRM.Domain.Categories;
using MediatR;

namespace CRM.Application.Admin.Categories.Queries;

public record ListCategoriesQuery : IRequest<IReadOnlyList<CategoryDto>>;

public class ListCategoriesQueryHandler
    : IRequestHandler<ListCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ICategoryRepository _categories;
    public ListCategoriesQueryHandler(ICategoryRepository categories) => _categories = categories;

    public async Task<IReadOnlyList<CategoryDto>> Handle(
        ListCategoriesQuery query, CancellationToken ct)
    {
        var all = await _categories.ListAllAsync(ct);
        var parents = all.Where(c => c.ParentCategoryId == null).ToList();

        return parents.Select(p =>
        {
            var children = all
                .Where(c => c.ParentCategoryId == p.Id)
                .Select(c => CreateCategoryCommandHandler.Map(c))
                .ToList();
            return CreateCategoryCommandHandler.Map(p, children);
        }).ToList();
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CategoryCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Create AdminCategoriesController**

```csharp
// src/CRM.API/Controllers/AdminCategoriesController.cs
using CRM.Application.Admin.Categories.Commands;
using CRM.Application.Admin.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class AdminCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminCategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(new { data = await _mediator.Send(new ListCategoriesQuery(), ct) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CategoryRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new CreateCategoryCommand(req.Name, req.NameAr, req.ParentId, req.SortOrder), ct);
            return StatusCode(201, new { data = result });
        }
        catch (InvalidOperationException ex)
            { return UnprocessableEntity(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCategoryRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateCategoryCommand(id, req.Name, req.NameAr, req.SortOrder), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeactivateCategoryCommand(id), ct);
            return Ok(new { data = new { id, isActive = false } });
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
            await _mediator.Send(new ReactivateCategoryCommand(id), ct);
            return Ok(new { data = new { id, isActive = true } });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}

public record CategoryRequest(string Name, string? NameAr, Guid? ParentId, int SortOrder);
public record UpdateCategoryRequest(string? Name, string? NameAr, int? SortOrder);
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminCategoriesControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Categories.Commands;
using CRM.Application.Admin.Categories.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminCategoriesControllerTests
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
    public async Task Create_ChildOfChild_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateCategoryCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Maximum category depth is 1."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/categories",
            new { name = "Grandchild", parentId = Guid.NewGuid(), sortOrder = 1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateCategoryCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Cannot deactivate: 3 open tickets."));

        var response = await BuildClient()
            .PostAsync($"/api/admin/categories/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminCategoriesControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Domain/Categories/ \
        src/CRM.Application/Admin/Categories/ \
        src/CRM.API/Controllers/AdminCategoriesController.cs \
        tests/CRM.Application.Tests/Admin/CategoryCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminCategoriesControllerTests.cs
git commit -m "feat(admin): add Category CRUD with two-level depth enforcement and cascade-deactivate of children"
```
