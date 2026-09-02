# Personal Agent Tasks CRUD — Implementation Plan

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

**Story:** US-BE-062  
**Goal:** Implement `GET/POST/PUT/DELETE /api/agents/me/tasks` — CRUD for personal agent tasks. Maximum 200 incomplete tasks per agent (BR-AGT-019). A nightly Hangfire job purges completed tasks older than 30 days (BR-AGT-018). Tasks support optional `ticketId` and `customerId` links.

**Architecture:** `AgentTask` entity with `AgentTaskStatus` and `AgentTaskPriority` enums. `IAgentTaskRepository` for persistence. `CreateAgentTaskCommand` enforces the 200-task cap. `PurgeCompletedTasksJob` registered as `Hangfire.RecurringJob` at `"0 2 * * *"` (2 AM daily). Sort order: incomplete first, then `dueAt ASC`, then `createdAt ASC`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, Hangfire, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Agents/AgentTask.cs` |
| Create | `src/CRM.Domain/Agents/AgentTaskStatus.cs` |
| Create | `src/CRM.Domain/Agents/AgentTaskPriority.cs` |
| Create | `src/CRM.Domain/Agents/IAgentTaskRepository.cs` |
| Create | `src/CRM.Application/Agents/Commands/CreateAgentTaskCommand.cs` |
| Create | `src/CRM.Application/Agents/Commands/UpdateAgentTaskCommand.cs` |
| Create | `src/CRM.Application/Agents/Commands/DeleteAgentTaskCommand.cs` |
| Create | `src/CRM.Application/Agents/Queries/ListMyTasksQuery.cs` |
| Create | `src/CRM.Application/Agents/DTOs/AgentTaskDto.cs` |
| Create | `src/CRM.Application/Agents/Jobs/PurgeCompletedTasksJob.cs` |
| Modify | `src/CRM.API/Controllers/AgentMeController.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.Application.Tests/Agents/AgentTaskCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Agents/AgentMeControllerTaskTests.cs` |

---

## Task 1: AgentTask Entity + CRUD Commands + Purge Job

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Agents/AgentTaskCommandHandlerTests.cs
using CRM.Application.Agents.Commands;
using CRM.Application.Agents.Queries;
using CRM.Application.Common;
using CRM.Domain.Agents;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Agents;

public class AgentTaskCommandHandlerTests
{
    private readonly Mock<IAgentTaskRepository> _repo = new();
    private readonly CreateAgentTaskCommandHandler _createHandler;
    private readonly UpdateAgentTaskCommandHandler _updateHandler;
    private readonly DeleteAgentTaskCommandHandler _deleteHandler;
    private readonly ListMyTasksQueryHandler _listHandler;

    public AgentTaskCommandHandlerTests()
    {
        _createHandler = new CreateAgentTaskCommandHandler(_repo.Object);
        _updateHandler = new UpdateAgentTaskCommandHandler(_repo.Object);
        _deleteHandler = new DeleteAgentTaskCommandHandler(_repo.Object);
        _listHandler = new ListMyTasksQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Create_ValidTask_PersistsIt()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.CountIncompleteAsync(agentId, default)).ReturnsAsync(0);

        var result = await _createHandler.Handle(
            new CreateAgentTaskCommand(
                agentId, "Follow up Sara", "Call at 2pm",
                AgentTaskPriority.High, DateTime.UtcNow.AddDays(1),
                null, null),
            default);

        Assert.Equal("Follow up Sara", result.Title);
        Assert.Equal("Pending", result.Status);
        _repo.Verify(r => r.AddAsync(It.IsAny<AgentTask>(), default), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Create_At200TaskLimit_ThrowsValidationException()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.CountIncompleteAsync(agentId, default)).ReturnsAsync(200);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _createHandler.Handle(
                new CreateAgentTaskCommand(
                    agentId, "Task 201", null,
                    AgentTaskPriority.Low, null, null, null),
                default));

        Assert.Contains("MAX_TASKS_REACHED", ex.Message);
    }

    [Fact]
    public async Task Update_OtherAgentTask_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var task = AgentTask.Create(ownerId, "Task", null, AgentTaskPriority.Low, null, null, null);
        _repo.Setup(r => r.FindByIdAsync(task.Id, default)).ReturnsAsync(task);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _updateHandler.Handle(
                new UpdateAgentTaskCommand(task.Id, otherId, null, null, null, null, null),
                default));
    }

    [Fact]
    public async Task Delete_OtherAgentTask_ThrowsUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var task = AgentTask.Create(ownerId, "Task", null, AgentTaskPriority.Low, null, null, null);
        _repo.Setup(r => r.FindByIdAsync(task.Id, default)).ReturnsAsync(task);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _deleteHandler.Handle(
                new DeleteAgentTaskCommand(task.Id, otherId), default));
    }

    [Fact]
    public async Task List_ReturnsTasksForAgent()
    {
        var agentId = Guid.NewGuid();
        _repo.Setup(r => r.ListAsync(agentId, null, null, null, false, 1, 20, default))
             .ReturnsAsync(new PagedResult<AgentTask>(new List<AgentTask>(), 0, 1, 20));

        var result = await _listHandler.Handle(
            new ListMyTasksQuery(agentId, null, null, null, false, 1, 20), default);

        Assert.Equal(0, result.TotalCount);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AgentTaskCommandHandlerTests" -v n
```

Expected: FAIL — `AgentTask` entity and commands do not exist yet.

- [ ] **Step 3: Create AgentTaskStatus and AgentTaskPriority enums**

```csharp
// src/CRM.Domain/Agents/AgentTaskStatus.cs
namespace CRM.Domain.Agents;

public enum AgentTaskStatus { Pending, InProgress, Completed }
```

```csharp
// src/CRM.Domain/Agents/AgentTaskPriority.cs
namespace CRM.Domain.Agents;

public enum AgentTaskPriority { High, Medium, Low }
```

- [ ] **Step 4: Create AgentTask entity**

```csharp
// src/CRM.Domain/Agents/AgentTask.cs
namespace CRM.Domain.Agents;

public class AgentTask
{
    public Guid Id { get; private set; }
    public Guid AgentId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AgentTaskPriority Priority { get; private set; }
    public AgentTaskStatus Status { get; private set; }
    public DateTime? DueAt { get; private set; }
    public Guid? TicketId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private AgentTask() { }

    public static AgentTask Create(
        Guid agentId, string title, string? description,
        AgentTaskPriority priority, DateTime? dueAt,
        Guid? ticketId, Guid? customerId)
        => new()
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            Title = title,
            Description = description,
            Priority = priority,
            Status = AgentTaskStatus.Pending,
            DueAt = dueAt,
            TicketId = ticketId,
            CustomerId = customerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string? title, string? description,
        AgentTaskPriority? priority, AgentTaskStatus? status, DateTime? dueAt)
    {
        if (title is not null) Title = title;
        if (description is not null) Description = description;
        if (priority is not null) Priority = priority.Value;
        if (status is not null)
        {
            Status = status.Value;
            if (status == AgentTaskStatus.Completed)
                CompletedAt = DateTime.UtcNow;
        }
        if (dueAt is not null) DueAt = dueAt;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 5: Create IAgentTaskRepository**

```csharp
// src/CRM.Domain/Agents/IAgentTaskRepository.cs
using CRM.Application.Common;

namespace CRM.Domain.Agents;

public interface IAgentTaskRepository
{
    Task<AgentTask?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<AgentTask>> ListAsync(
        Guid agentId,
        AgentTaskStatus? status,
        AgentTaskPriority? priority,
        Guid? ticketId,
        bool overdueOnly,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<int> CountIncompleteAsync(Guid agentId, CancellationToken ct = default);
    Task AddAsync(AgentTask task, CancellationToken ct = default);
    Task RemoveAsync(AgentTask task, CancellationToken ct = default);
    Task<int> PurgeCompletedOlderThanAsync(DateTime threshold, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 6: Create AgentTaskDto**

```csharp
// src/CRM.Application/Agents/DTOs/AgentTaskDto.cs
namespace CRM.Application.Agents.DTOs;

public record AgentTaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Priority,
    string Status,
    DateTime? DueAt,
    bool IsOverdue,
    Guid? TicketId,
    Guid? CustomerId,
    DateTime CreatedAt,
    DateTime? CompletedAt);
```

- [ ] **Step 7: Implement CreateAgentTaskCommand**

```csharp
// src/CRM.Application/Agents/Commands/CreateAgentTaskCommand.cs
using CRM.Application.Agents.DTOs;
using CRM.Domain.Agents;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record CreateAgentTaskCommand(
    Guid AgentId, string Title, string? Description,
    AgentTaskPriority Priority, DateTime? DueAt,
    Guid? TicketId, Guid? CustomerId)
    : IRequest<AgentTaskDto>;

public class CreateAgentTaskCommandHandler
    : IRequestHandler<CreateAgentTaskCommand, AgentTaskDto>
{
    private const int MaxIncompleteTasks = 200;
    private readonly IAgentTaskRepository _tasks;

    public CreateAgentTaskCommandHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task<AgentTaskDto> Handle(
        CreateAgentTaskCommand cmd, CancellationToken ct)
    {
        int count = await _tasks.CountIncompleteAsync(cmd.AgentId, ct);
        if (count >= MaxIncompleteTasks)
            throw new ValidationException("MAX_TASKS_REACHED: Maximum 200 incomplete tasks allowed.",
                new[] { new ValidationFailure("Tasks",
                    "Maximum 200 incomplete tasks reached.", "MAX_TASKS_REACHED") });

        var task = AgentTask.Create(
            cmd.AgentId, cmd.Title, cmd.Description,
            cmd.Priority, cmd.DueAt, cmd.TicketId, cmd.CustomerId);

        await _tasks.AddAsync(task, ct);
        await _tasks.SaveChangesAsync(ct);

        return Map(task);
    }

    internal static AgentTaskDto Map(AgentTask t)
        => new(t.Id, t.Title, t.Description, t.Priority.ToString(), t.Status.ToString(),
               t.DueAt, t.DueAt < DateTime.UtcNow && t.Status != AgentTaskStatus.Completed,
               t.TicketId, t.CustomerId, t.CreatedAt, t.CompletedAt);
}
```

- [ ] **Step 8: Implement UpdateAgentTaskCommand**

```csharp
// src/CRM.Application/Agents/Commands/UpdateAgentTaskCommand.cs
using CRM.Application.Agents.DTOs;
using CRM.Domain.Agents;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record UpdateAgentTaskCommand(
    Guid TaskId, Guid AgentId,
    string? Title, string? Description,
    AgentTaskPriority? Priority, AgentTaskStatus? Status, DateTime? DueAt)
    : IRequest<AgentTaskDto>;

public class UpdateAgentTaskCommandHandler
    : IRequestHandler<UpdateAgentTaskCommand, AgentTaskDto>
{
    private readonly IAgentTaskRepository _tasks;

    public UpdateAgentTaskCommandHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task<AgentTaskDto> Handle(
        UpdateAgentTaskCommand cmd, CancellationToken ct)
    {
        var task = await _tasks.FindByIdAsync(cmd.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (task.AgentId != cmd.AgentId)
            throw new UnauthorizedAccessException("You can only update your own tasks.");

        task.Update(cmd.Title, cmd.Description, cmd.Priority, cmd.Status, cmd.DueAt);
        await _tasks.SaveChangesAsync(ct);

        return CreateAgentTaskCommandHandler.Map(task);
    }
}
```

- [ ] **Step 9: Implement DeleteAgentTaskCommand**

```csharp
// src/CRM.Application/Agents/Commands/DeleteAgentTaskCommand.cs
using CRM.Domain.Agents;
using MediatR;

namespace CRM.Application.Agents.Commands;

public record DeleteAgentTaskCommand(Guid TaskId, Guid AgentId) : IRequest;

public class DeleteAgentTaskCommandHandler : IRequestHandler<DeleteAgentTaskCommand>
{
    private readonly IAgentTaskRepository _tasks;

    public DeleteAgentTaskCommandHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task Handle(DeleteAgentTaskCommand cmd, CancellationToken ct)
    {
        var task = await _tasks.FindByIdAsync(cmd.TaskId, ct)
            ?? throw new KeyNotFoundException($"Task {cmd.TaskId} not found.");

        if (task.AgentId != cmd.AgentId)
            throw new UnauthorizedAccessException("You can only delete your own tasks.");

        await _tasks.RemoveAsync(task, ct);
        await _tasks.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 10: Implement ListMyTasksQuery**

```csharp
// src/CRM.Application/Agents/Queries/ListMyTasksQuery.cs
using CRM.Application.Agents.DTOs;
using CRM.Application.Common;
using CRM.Domain.Agents;
using MediatR;

namespace CRM.Application.Agents.Queries;

public record ListMyTasksQuery(
    Guid AgentId,
    AgentTaskStatus? Status,
    AgentTaskPriority? Priority,
    Guid? TicketId,
    bool OverdueOnly,
    int Page,
    int PageSize)
    : IRequest<PagedResult<AgentTaskDto>>;

public class ListMyTasksQueryHandler
    : IRequestHandler<ListMyTasksQuery, PagedResult<AgentTaskDto>>
{
    private readonly IAgentTaskRepository _tasks;

    public ListMyTasksQueryHandler(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task<PagedResult<AgentTaskDto>> Handle(
        ListMyTasksQuery query, CancellationToken ct)
    {
        var paged = await _tasks.ListAsync(
            query.AgentId, query.Status, query.Priority, query.TicketId,
            query.OverdueOnly, query.Page, query.PageSize, ct);

        var dtos = paged.Items
            .Select(CreateAgentTaskCommandHandler.Map)
            .ToList();

        return new PagedResult<AgentTaskDto>(dtos, paged.TotalCount, paged.Page, paged.PageSize);
    }
}
```

- [ ] **Step 11: Implement PurgeCompletedTasksJob**

```csharp
// src/CRM.Application/Agents/Jobs/PurgeCompletedTasksJob.cs
using CRM.Domain.Agents;

namespace CRM.Application.Agents.Jobs;

public class PurgeCompletedTasksJob
{
    private readonly IAgentTaskRepository _tasks;

    public PurgeCompletedTasksJob(IAgentTaskRepository tasks) => _tasks = tasks;

    public async Task Execute(CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(-30);
        await _tasks.PurgeCompletedOlderThanAsync(threshold, ct);
    }
}
```

Register in `src/CRM.API/Program.cs`:
```csharp
RecurringJob.AddOrUpdate<PurgeCompletedTasksJob>(
    "purge-completed-tasks",
    job => job.Execute(CancellationToken.None),
    "0 2 * * *"); // 2 AM daily
```

- [ ] **Step 12: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "AgentTaskCommandHandlerTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 13: Add task endpoints to AgentMeController**

```csharp
// Add to src/CRM.API/Controllers/AgentMeController.cs:

[HttpGet("tasks")]
public async Task<IActionResult> ListTasks(
    [FromQuery] string? status,
    [FromQuery] string? priority,
    [FromQuery] Guid? ticketId,
    [FromQuery] bool overdue = false,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken ct = default)
{
    var parsedStatus = Enum.TryParse<AgentTaskStatus>(status, out var s) ? s : (AgentTaskStatus?)null;
    var parsedPriority = Enum.TryParse<AgentTaskPriority>(priority, out var p) ? p : (AgentTaskPriority?)null;

    var result = await _mediator.Send(
        new ListMyTasksQuery(CurrentUserId, parsedStatus, parsedPriority,
            ticketId, overdue, page, pageSize), ct);
    return Ok(result);
}

[HttpPost("tasks")]
public async Task<IActionResult> CreateTask(
    [FromBody] CreateTaskRequest req, CancellationToken ct)
{
    var priority = Enum.TryParse<AgentTaskPriority>(req.Priority, out var p)
        ? p : AgentTaskPriority.Medium;

    var result = await _mediator.Send(
        new CreateAgentTaskCommand(
            CurrentUserId, req.Title, req.Description,
            priority, req.DueAt, req.TicketId, req.CustomerId), ct);

    return StatusCode(201, result);
}

[HttpPut("tasks/{id:guid}")]
public async Task<IActionResult> UpdateTask(
    Guid id, [FromBody] UpdateTaskRequest req, CancellationToken ct)
{
    try
    {
        var priority = req.Priority is not null &&
            Enum.TryParse<AgentTaskPriority>(req.Priority, out var p)
            ? p : (AgentTaskPriority?)null;
        var taskStatus = req.Status is not null &&
            Enum.TryParse<AgentTaskStatus>(req.Status, out var s)
            ? s : (AgentTaskStatus?)null;

        var result = await _mediator.Send(
            new UpdateAgentTaskCommand(
                id, CurrentUserId, req.Title, req.Description,
                priority, taskStatus, req.DueAt), ct);
        return Ok(result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
}

[HttpDelete("tasks/{id:guid}")]
public async Task<IActionResult> DeleteTask(Guid id, CancellationToken ct)
{
    try
    {
        await _mediator.Send(new DeleteAgentTaskCommand(id, CurrentUserId), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
}

public record CreateTaskRequest(string Title, string? Description, string? Priority,
    DateTime? DueAt, Guid? TicketId, Guid? CustomerId);
public record UpdateTaskRequest(string? Title, string? Description, string? Priority,
    string? Status, DateTime? DueAt);
```

- [ ] **Step 14: Write controller test**

```csharp
// tests/CRM.API.Tests/Agents/AgentMeControllerTaskTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Agents.Commands;
using CRM.Application.Agents.DTOs;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Agents;

public class AgentMeControllerTaskTests
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
    public async Task CreateTask_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateAgentTaskCommand>(), default))
                 .ReturnsAsync(new AgentTaskDto(
                     Guid.NewGuid(), "Follow up", null, "High", "Pending",
                     null, false, null, null, DateTime.UtcNow, null));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/agents/me/tasks",
            new { title = "Follow up", priority = "High" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_AtLimit_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateAgentTaskCommand>(), default))
                 .ThrowsAsync(new ValidationException("MAX_TASKS_REACHED",
                     new[] { new ValidationFailure("Tasks", "MAX_TASKS_REACHED") }));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/agents/me/tasks",
            new { title = "Task 201", priority = "Low" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
```

- [ ] **Step 15: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AgentMeControllerTaskTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 16: Commit**

```bash
git add src/CRM.Domain/Agents/ \
        src/CRM.Application/Agents/Commands/CreateAgentTaskCommand.cs \
        src/CRM.Application/Agents/Commands/UpdateAgentTaskCommand.cs \
        src/CRM.Application/Agents/Commands/DeleteAgentTaskCommand.cs \
        src/CRM.Application/Agents/Queries/ListMyTasksQuery.cs \
        src/CRM.Application/Agents/DTOs/AgentTaskDto.cs \
        src/CRM.Application/Agents/Jobs/PurgeCompletedTasksJob.cs \
        src/CRM.API/Controllers/AgentMeController.cs \
        src/CRM.API/Program.cs \
        tests/CRM.Application.Tests/Agents/AgentTaskCommandHandlerTests.cs \
        tests/CRM.API.Tests/Agents/AgentMeControllerTaskTests.cs
git commit -m "feat(agents): add personal task CRUD with 200-task cap and nightly purge of 30-day-old completed tasks"
```
