# Global Quick-Reply Templates CRUD — Implementation Plan

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

**Story:** US-BE-071  
**Goal:** Implement `GET /api/admin/templates`, `POST /api/admin/templates`, `PUT /api/admin/templates/{id}`, and `DELETE /api/admin/templates/{id}` — Admin CRUD for Global-scope quick-reply templates. These templates are visible to all agents (returned by `GET /api/agents/me/templates`). Agents cannot edit or delete them (enforced in US-BE-060).

**Architecture:** Reuses `QuickReplyTemplate` entity and `IQuickReplyTemplateRepository` from US-BE-060. Adds `CreateGlobalTemplateCommand`, `UpdateGlobalTemplateCommand`, `DeleteGlobalTemplateCommand` in the admin layer. `GET` lists only Global-scope templates (agents' `ListMyTemplatesQuery` already includes them).

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Admin/Templates/Commands/CreateGlobalTemplateCommand.cs` |
| Create | `src/CRM.Application/Admin/Templates/Commands/UpdateGlobalTemplateCommand.cs` |
| Create | `src/CRM.Application/Admin/Templates/Commands/DeleteGlobalTemplateCommand.cs` |
| Create | `src/CRM.Application/Admin/Templates/Queries/ListGlobalTemplatesQuery.cs` |
| Create | `src/CRM.API/Controllers/AdminTemplatesController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/GlobalTemplateCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminTemplatesControllerTests.cs` |

---

## Task 1: Global Template CRUD

> Note: `QuickReplyTemplate` entity and `IQuickReplyTemplateRepository` are defined in US-BE-060. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/GlobalTemplateCommandHandlerTests.cs
using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Admin.Templates.Queries;
using CRM.Application.Common;
using CRM.Domain.Templates;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class GlobalTemplateCommandHandlerTests
{
    private readonly Mock<IQuickReplyTemplateRepository> _repo = new();
    private readonly CreateGlobalTemplateCommandHandler _createHandler;
    private readonly UpdateGlobalTemplateCommandHandler _updateHandler;
    private readonly DeleteGlobalTemplateCommandHandler _deleteHandler;
    private readonly ListGlobalTemplatesQueryHandler _listHandler;

    public GlobalTemplateCommandHandlerTests()
    {
        _createHandler = new CreateGlobalTemplateCommandHandler(_repo.Object);
        _updateHandler = new UpdateGlobalTemplateCommandHandler(_repo.Object);
        _deleteHandler = new DeleteGlobalTemplateCommandHandler(_repo.Object);
        _listHandler = new ListGlobalTemplatesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_GlobalTemplate_SetsGlobalScope()
    {
        var adminId = Guid.NewGuid();

        var result = await _createHandler.Handle(
            new CreateGlobalTemplateCommand(adminId, "Standard Greeting", "Hello {{customer_name}}!", "Greeting"),
            default);

        Assert.Equal("Global", result.Scope);
        Assert.Equal("Standard Greeting", result.Title);
        _repo.Verify(r => r.AddAsync(It.IsAny<QuickReplyTemplate>(), default), Times.Once);
    }

    [Fact]
    public async Task Update_GlobalTemplate_ChangesTitle()
    {
        var adminId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Old Title", "Content", "Greeting", adminId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        var result = await _updateHandler.Handle(
            new UpdateGlobalTemplateCommand(template.Id, "New Title", null, null),
            default);

        Assert.Equal("New Title", result.Title);
    }

    [Fact]
    public async Task Update_PersonalTemplate_ThrowsInvalidOperationException()
    {
        var adminId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Personal Template", "Content", "Greeting", adminId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _updateHandler.Handle(
                new UpdateGlobalTemplateCommand(template.Id, "New Title", null, null),
                default));
    }

    [Fact]
    public async Task Delete_GlobalTemplate_RemovesIt()
    {
        var adminId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Title", "Content", "Cat", adminId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await _deleteHandler.Handle(
            new DeleteGlobalTemplateCommand(template.Id), default);

        _repo.Verify(r => r.RemoveAsync(template, default), Times.Once);
    }

    [Fact]
    public async Task Delete_PersonalTemplate_ThrowsInvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "Personal", "Content", "Cat", agentId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deleteHandler.Handle(
                new DeleteGlobalTemplateCommand(template.Id), default));
    }

    [Fact]
    public async Task List_ReturnsOnlyGlobalTemplates()
    {
        _repo.Setup(r => r.ListForAgentAsync(
            It.IsAny<Guid>(), TemplateScope.Global, null, 1, 20, default))
             .ReturnsAsync(new PagedResult<QuickReplyTemplate>(
                 new List<QuickReplyTemplate>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListGlobalTemplatesQuery(null, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GlobalTemplateCommandHandlerTests" -v n
```

Expected: FAIL — `CreateGlobalTemplateCommand` does not exist yet.

- [ ] **Step 3: Implement CreateGlobalTemplateCommand**

```csharp
// src/CRM.Application/Admin/Templates/Commands/CreateGlobalTemplateCommand.cs
using CRM.Application.Agents.DTOs;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Commands;

public record CreateGlobalTemplateCommand(
    Guid AdminId, string Title, string Content, string? Category)
    : IRequest<TemplateDto>;

public class CreateGlobalTemplateCommandHandler
    : IRequestHandler<CreateGlobalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public CreateGlobalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(
        CreateGlobalTemplateCommand cmd, CancellationToken ct)
    {
        var template = QuickReplyTemplate.CreateGlobal(
            cmd.Title, cmd.Content, cmd.Category, cmd.AdminId);

        await _templates.AddAsync(template, ct);
        await _templates.SaveChangesAsync(ct);

        return CreatePersonalTemplateCommandHandler.Map(template);
    }
}
```

- [ ] **Step 4: Implement UpdateGlobalTemplateCommand**

```csharp
// src/CRM.Application/Admin/Templates/Commands/UpdateGlobalTemplateCommand.cs
using CRM.Application.Agents.DTOs;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Commands;

public record UpdateGlobalTemplateCommand(
    Guid TemplateId, string? Title, string? Content, string? Category)
    : IRequest<TemplateDto>;

public class UpdateGlobalTemplateCommandHandler
    : IRequestHandler<UpdateGlobalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public UpdateGlobalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(UpdateGlobalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope != TemplateScope.Global)
            throw new InvalidOperationException(
                "Only Global templates can be edited via this endpoint.");

        template.Update(cmd.Title, cmd.Content, cmd.Category);
        await _templates.SaveChangesAsync(ct);

        return CreatePersonalTemplateCommandHandler.Map(template);
    }
}
```

- [ ] **Step 5: Implement DeleteGlobalTemplateCommand**

```csharp
// src/CRM.Application/Admin/Templates/Commands/DeleteGlobalTemplateCommand.cs
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Commands;

public record DeleteGlobalTemplateCommand(Guid TemplateId) : IRequest;

public class DeleteGlobalTemplateCommandHandler : IRequestHandler<DeleteGlobalTemplateCommand>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public DeleteGlobalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task Handle(DeleteGlobalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope != TemplateScope.Global)
            throw new InvalidOperationException(
                "Only Global templates can be deleted via this admin endpoint.");

        await _templates.RemoveAsync(template, ct);
        await _templates.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 6: Implement ListGlobalTemplatesQuery**

```csharp
// src/CRM.Application/Admin/Templates/Queries/ListGlobalTemplatesQuery.cs
using CRM.Application.Agents.DTOs;
using CRM.Application.Common;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Admin.Templates.Queries;

public record ListGlobalTemplatesQuery(string? Search, int Page, int PageSize)
    : IRequest<PagedResult<TemplateDto>>;

public class ListGlobalTemplatesQueryHandler
    : IRequestHandler<ListGlobalTemplatesQuery, PagedResult<TemplateDto>>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public ListGlobalTemplatesQueryHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<PagedResult<TemplateDto>> Handle(
        ListGlobalTemplatesQuery query, CancellationToken ct)
    {
        // Use a sentinel userId — ListForAgentAsync returns Personal for that agent + all Global
        // For admin list, we just want Global; pass a zero GUID as agentId and filter by Global scope
        var paged = await _templates.ListForAgentAsync(
            Guid.Empty, TemplateScope.Global, query.Search, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(CreatePersonalTemplateCommandHandler.Map)
            .ToList();

        return new PagedResult<TemplateDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GlobalTemplateCommandHandlerTests" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 8: Create AdminTemplatesController**

```csharp
// src/CRM.API/Controllers/AdminTemplatesController.cs
using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Admin.Templates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/templates")]
[Authorize(Roles = "Admin")]
public class AdminTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public AdminTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new ListGlobalTemplatesQuery(search, page, pageSize), ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] GlobalTemplateRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateGlobalTemplateCommand(CurrentUserId, req.Title, req.Content, req.Category), ct);
        return StatusCode(201, new { data = result });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] GlobalTemplateRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(
                new UpdateGlobalTemplateCommand(id, req.Title, req.Content, req.Category), ct);
            return Ok(new { data = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _mediator.Send(new DeleteGlobalTemplateCommand(id), ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex)
            { return StatusCode(403, new { error = ex.Message }); }
    }
}

public record GlobalTemplateRequest(string? Title, string? Content, string? Category);
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminTemplatesControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Agents.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminTemplatesControllerTests
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
    public async Task Create_GlobalTemplate_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateGlobalTemplateCommand>(), default))
                 .ReturnsAsync(new TemplateDto(
                     Guid.NewGuid(), "Standard Greeting", "Hello!", "Greeting",
                     "Global", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/admin/templates",
            new { title = "Standard Greeting", content = "Hello!", category = "Greeting" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Delete_PersonalTemplate_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteGlobalTemplateCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException(
                     "Only Global templates can be deleted via this admin endpoint."));

        var response = await BuildClient()
            .DeleteAsync($"/api/admin/templates/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminTemplatesControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Application/Admin/Templates/ \
        src/CRM.API/Controllers/AdminTemplatesController.cs \
        tests/CRM.Application.Tests/Admin/GlobalTemplateCommandHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminTemplatesControllerTests.cs
git commit -m "feat(admin): add Global Quick-Reply Template CRUD — GET/POST/PUT/DELETE /api/admin/templates"
```
