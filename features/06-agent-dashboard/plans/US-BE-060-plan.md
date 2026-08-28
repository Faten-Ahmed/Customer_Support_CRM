# Personal Quick-Reply Templates CRUD — Implementation Plan

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

**Story:** US-BE-060  
**Goal:** Implement `GET /api/agents/me/templates`, `POST /api/agents/me/templates`, `PUT /api/agents/me/templates/{id}`, and `DELETE /api/agents/me/templates/{id}` — CRUD for Personal-scope quick-reply templates. Agents cannot edit or delete Global-scope templates (those are managed in US-BE-071). `GET` returns both Personal and Global templates visible to the caller.

**Architecture:** `QuickReplyTemplate` entity with `TemplateScope` enum (Personal/Global). `IQuickReplyTemplateRepository` provides persistence. Commands enforce scope ownership: `PUT` and `DELETE` throw `InvalidOperationException` for Global templates. `GET` returns caller's Personal + all Global.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Templates/QuickReplyTemplate.cs` |
| Create | `src/CRM.Domain/Templates/TemplateScope.cs` |
| Create | `src/CRM.Domain/Templates/IQuickReplyTemplateRepository.cs` |
| Create | `src/CRM.Application/Agents/Queries/ListMyTemplatesQuery.cs` |
| Create | `src/CRM.Application/Agents/Commands/CreatePersonalTemplateCommand.cs` |
| Create | `src/CRM.Application/Agents/Commands/UpdatePersonalTemplateCommand.cs` |
| Create | `src/CRM.Application/Agents/Commands/DeletePersonalTemplateCommand.cs` |
| Create | `src/CRM.Application/Agents/DTOs/TemplateDto.cs` |
| Modify | `src/CRM.API/Controllers/AgentMeController.cs` |
| Test   | `tests/CRM.Application.Tests/Agents/PersonalTemplateCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Agents/AgentMeControllerTemplateTests.cs` |

---

## Task 1: QuickReplyTemplate Entity + CRUD Commands

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Agents/PersonalTemplateCommandHandlerTests.cs
using CRM.Application.Agents.Commands;
using CRM.Application.Agents.Queries;
using CRM.Application.Common;
using CRM.Domain.Templates;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class PersonalTemplateCommandHandlerTests
{
    private readonly Mock<IQuickReplyTemplateRepository> _repo = new();
    private readonly CreatePersonalTemplateCommandHandler _createHandler;
    private readonly UpdatePersonalTemplateCommandHandler _updateHandler;
    private readonly DeletePersonalTemplateCommandHandler _deleteHandler;
    private readonly ListMyTemplatesQueryHandler _listHandler;

    public PersonalTemplateCommandHandlerTests()
    {
        _createHandler = new CreatePersonalTemplateCommandHandler(_repo.Object);
        _updateHandler = new UpdatePersonalTemplateCommandHandler(_repo.Object);
        _deleteHandler = new DeletePersonalTemplateCommandHandler(_repo.Object);
        _listHandler = new ListMyTemplatesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_PersonalTemplate_ReturnsDto()
    {
        var agentId = Guid.NewGuid();

        var result = await _createHandler.Handle(
            new CreatePersonalTemplateCommand(
                agentId, "My Greeting", "Hello {{customer_name}}!", "Greeting"),
            default);

        Assert.Equal("Personal", result.Scope);
        Assert.Equal("My Greeting", result.Title);
        _repo.Verify(r => r.AddAsync(It.IsAny<QuickReplyTemplate>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Update_GlobalTemplate_ThrowsInvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Global Greeting", "Hello!", "Greeting", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _updateHandler.Handle(
                new UpdatePersonalTemplateCommand(
                    template.Id, agentId, "New Title", null, null),
                default));
    }

    [Fact]
    public async Task Update_OtherAgentPersonalTemplate_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreatePersonal(
            "My Template", "Content", "Cat", ownerId);
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _updateHandler.Handle(
                new UpdatePersonalTemplateCommand(
                    template.Id, otherId, "New Title", null, null),
                default));
    }

    [Fact]
    public async Task Delete_GlobalTemplate_ThrowsInvalidOperationException()
    {
        var agentId = Guid.NewGuid();
        var template = QuickReplyTemplate.CreateGlobal(
            "Global", "Content", "Cat", Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(template.Id, default)).ReturnsAsync(template);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _deleteHandler.Handle(
                new DeletePersonalTemplateCommand(template.Id, agentId), default));
    }

    [Fact]
    public async Task List_ReturnsPersonalAndGlobalTemplates()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.ListForAgentAsync(agentId, null, null, 1, 20, default))
             .ReturnsAsync(new PagedResult<QuickReplyTemplate>(
                 new List<QuickReplyTemplate>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListMyTemplatesQuery(agentId, null, null, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PersonalTemplateCommandHandlerTests" -v n
```

Expected: FAIL — entities and commands do not exist yet.

- [ ] **Step 3: Create TemplateScope enum**

```csharp
// src/CRM.Domain/Templates/TemplateScope.cs
namespace CRM.Domain.Templates;

public enum TemplateScope { Personal, Global }
```

- [ ] **Step 4: Create QuickReplyTemplate entity**

```csharp
// src/CRM.Domain/Templates/QuickReplyTemplate.cs
namespace CRM.Domain.Templates;

public class QuickReplyTemplate
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public TemplateScope Scope { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private QuickReplyTemplate() { }

    public static QuickReplyTemplate CreatePersonal(
        string title, string content, string? category, Guid agentId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Category = category,
            Scope = TemplateScope.Personal,
            CreatedByUserId = agentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static QuickReplyTemplate CreateGlobal(
        string title, string content, string? category, Guid adminId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Category = category,
            Scope = TemplateScope.Global,
            CreatedByUserId = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string? title, string? content, string? category)
    {
        if (title is not null) Title = title;
        if (content is not null) Content = content;
        if (category is not null) Category = category;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 5: Create IQuickReplyTemplateRepository**

```csharp
// src/CRM.Domain/Templates/IQuickReplyTemplateRepository.cs
using CRM.Application.Common;

namespace CRM.Domain.Templates;

public interface IQuickReplyTemplateRepository
{
    Task<QuickReplyTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<QuickReplyTemplate>> ListForAgentAsync(
        Guid agentId, TemplateScope? scope, string? search,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(QuickReplyTemplate template, CancellationToken ct = default);
    Task RemoveAsync(QuickReplyTemplate template, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 6: Create TemplateDto**

```csharp
// src/CRM.Application/Agents/DTOs/TemplateDto.cs
namespace CRM.Application.Agents.DTOs;

public record TemplateDto(
    Guid Id,
    string Title,
    string Content,
    string? Category,
    string Scope,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 7: Implement CreatePersonalTemplateCommand**

```csharp
// src/CRM.Application/Agents/Commands/CreatePersonalTemplateCommand.cs
using CRM.Application.Agents.DTOs;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record CreatePersonalTemplateCommand(
    Guid AgentId, string Title, string Content, string? Category)
    : IRequest<TemplateDto>;

public class CreatePersonalTemplateCommandHandler
    : IRequestHandler<CreatePersonalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public CreatePersonalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(
        CreatePersonalTemplateCommand cmd, CancellationToken ct)
    {
        var template = QuickReplyTemplate.CreatePersonal(
            cmd.Title, cmd.Content, cmd.Category, cmd.AgentId);

        await _templates.AddAsync(template, ct);
        await _templates.SaveChangesAsync(ct);

        return Map(template);
    }

    internal static TemplateDto Map(QuickReplyTemplate t)
        => new(t.Id, t.Title, t.Content, t.Category, t.Scope.ToString(),
               t.CreatedByUserId, t.CreatedAt, t.UpdatedAt);
}
```

- [ ] **Step 8: Implement UpdatePersonalTemplateCommand**

```csharp
// src/CRM.Application/Agents/Commands/UpdatePersonalTemplateCommand.cs
using CRM.Application.Agents.DTOs;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record UpdatePersonalTemplateCommand(
    Guid TemplateId, Guid AgentId,
    string? Title, string? Content, string? Category)
    : IRequest<TemplateDto>;

public class UpdatePersonalTemplateCommandHandler
    : IRequestHandler<UpdatePersonalTemplateCommand, TemplateDto>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public UpdatePersonalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<TemplateDto> Handle(
        UpdatePersonalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope == TemplateScope.Global)
            throw new InvalidOperationException(
                "Global templates cannot be edited via this endpoint.");

        if (template.CreatedByUserId != cmd.AgentId)
            throw new UnauthorizedAccessException(
                "You can only edit your own personal templates.");

        template.Update(cmd.Title, cmd.Content, cmd.Category);
        await _templates.SaveChangesAsync(ct);

        return CreatePersonalTemplateCommandHandler.Map(template);
    }
}
```

- [ ] **Step 9: Implement DeletePersonalTemplateCommand**

```csharp
// src/CRM.Application/Agents/Commands/DeletePersonalTemplateCommand.cs
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record DeletePersonalTemplateCommand(Guid TemplateId, Guid AgentId) : IRequest;

public class DeletePersonalTemplateCommandHandler
    : IRequestHandler<DeletePersonalTemplateCommand>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public DeletePersonalTemplateCommandHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task Handle(DeletePersonalTemplateCommand cmd, CancellationToken ct)
    {
        var template = await _templates.FindByIdAsync(cmd.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template {cmd.TemplateId} not found.");

        if (template.Scope == TemplateScope.Global)
            throw new InvalidOperationException(
                "Global templates cannot be deleted via this endpoint.");

        if (template.CreatedByUserId != cmd.AgentId)
            throw new UnauthorizedAccessException(
                "You can only delete your own personal templates.");

        await _templates.RemoveAsync(template, ct);
        await _templates.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 10: Implement ListMyTemplatesQuery**

```csharp
// src/CRM.Application/Agents/Queries/ListMyTemplatesQuery.cs
using CRM.Application.Agents.DTOs;
using CRM.Application.Common;
using CRM.Domain.Templates;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record ListMyTemplatesQuery(
    Guid AgentId, TemplateScope? Scope, string? Search,
    int Page, int PageSize)
    : IRequest<PagedResult<TemplateDto>>;

public class ListMyTemplatesQueryHandler
    : IRequestHandler<ListMyTemplatesQuery, PagedResult<TemplateDto>>
{
    private readonly IQuickReplyTemplateRepository _templates;

    public ListMyTemplatesQueryHandler(IQuickReplyTemplateRepository templates)
        => _templates = templates;

    public async Task<PagedResult<TemplateDto>> Handle(
        ListMyTemplatesQuery query, CancellationToken ct)
    {
        var paged = await _templates.ListForAgentAsync(
            query.AgentId, query.Scope, query.Search,
            query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(CreatePersonalTemplateCommandHandler.Map)
            .ToList();

        return new PagedResult<TemplateDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 11: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PersonalTemplateCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 12: Add template endpoints to AgentMeController**

```csharp
// Add to src/CRM.API/Controllers/AgentMeController.cs:

[HttpGet("templates")]
public async Task<IActionResult> ListTemplates(
    [FromQuery] string? scope,
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    TemplateScope? parsedScope = Enum.TryParse<TemplateScope>(scope, out var s) ? s : null;
    var result = await _mediator.Send(
        new ListMyTemplatesQuery(CurrentUserId, parsedScope, search, page, pageSize), ct);
    return Ok(result);
}

[HttpPost("templates")]
public async Task<IActionResult> CreateTemplate(
    [FromBody] CreateTemplateRequest req, CancellationToken ct)
{
    var result = await _mediator.Send(
        new CreatePersonalTemplateCommand(CurrentUserId, req.Title, req.Content, req.Category), ct);
    return CreatedAtAction(nameof(ListTemplates), new { }, result);
}

[HttpPut("templates/{id:guid}")]
public async Task<IActionResult> UpdateTemplate(
    Guid id, [FromBody] UpdateTemplateRequest req, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(
            new UpdatePersonalTemplateCommand(id, CurrentUserId, req.Title, req.Content, req.Category), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return StatusCode(403, new { error = ex.Message }); }
}

[HttpDelete("templates/{id:guid}")]
public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new DeletePersonalTemplateCommand(id, CurrentUserId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return StatusCode(403, new { error = ex.Message }); }
}

public record CreateTemplateRequest(string Title, string Content, string? Category);
public record UpdateTemplateRequest(string? Title, string? Content, string? Category);
```

- [ ] **Step 13: Write controller test**

```csharp
// tests/CRM.API.Tests/Agents/AgentMeControllerTemplateTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Agents.Commands;
using CRM.Application.Agents.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Agents;

public class AgentMeControllerTemplateTests
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
    public async Task CreateTemplate_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreatePersonalTemplateCommand>(), default))
                 .ReturnsAsync(new TemplateDto(
                     Guid.NewGuid(), "Title", "Content", null, "Personal",
                     Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/agents/me/templates",
            new { title = "Title", content = "Content" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGlobalTemplate_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeletePersonalTemplateCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException(
                     "Global templates cannot be deleted via this endpoint."));

        var response = await BuildClient()
            .DeleteAsync($"/api/agents/me/templates/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 14: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AgentMeControllerTemplateTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 15: Commit**

```bash
git add src/CRM.Domain/Templates/ \
        src/CRM.Application/Agents/Commands/CreatePersonalTemplateCommand.cs \
        src/CRM.Application/Agents/Commands/UpdatePersonalTemplateCommand.cs \
        src/CRM.Application/Agents/Commands/DeletePersonalTemplateCommand.cs \
        src/CRM.Application/Agents/Queries/ListMyTemplatesQuery.cs \
        src/CRM.Application/Agents/DTOs/TemplateDto.cs \
        src/CRM.API/Controllers/AgentMeController.cs \
        tests/CRM.Application.Tests/Agents/PersonalTemplateCommandHandlerTests.cs \
        tests/CRM.API.Tests/Agents/AgentMeControllerTemplateTests.cs
git commit -m "feat(agents): add personal quick-reply template CRUD — GET/POST/PUT/DELETE /api/agents/me/templates"
```
