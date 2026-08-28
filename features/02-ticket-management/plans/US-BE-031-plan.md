# Delete Attachment — Implementation Plan

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

**Story:** US-BE-031  
**Goal:** Implement `DELETE /api/tickets/{id}/attachments/{attachmentId}` — removes the attachment from S3/MinIO and deletes the database record. Only Admin, Manager, or the agent who uploaded can delete.

**Architecture:** `DeleteAttachmentCommand(ticketId, attachmentId, requestingUserId, requestingUserRole)` → handler validates ownership (Admin/Manager bypass; Agent must be uploader), calls `IStorageService.DeleteAsync(key)`, removes the `Attachment` record, saves.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Tickets/Commands/DeleteAttachmentCommand.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/DeleteAttachmentCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerDeleteAttachmentTests.cs` |

---

## Task 1: DeleteAttachment Command + Handler

**Files:**
- Create: `src/CRM.Application/Tickets/Commands/DeleteAttachmentCommand.cs`
- Test: `tests/CRM.Application.Tests/Tickets/DeleteAttachmentCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/DeleteAttachmentCommandHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class DeleteAttachmentCommandHandlerTests
{
    private readonly Mock<IAttachmentRepository> _repo = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly DeleteAttachmentCommandHandler _handler;

    public DeleteAttachmentCommandHandlerTests()
    {
        _handler = new DeleteAttachmentCommandHandler(_repo.Object, _storage.Object);
    }

    private static Attachment MakeAttachment(Guid uploadedBy)
        => Attachment.Create(Guid.NewGuid(), "file.png", "image/png",
            1024, "tickets/file.png", uploadedBy);

    [Fact]
    public async Task Handle_AdminDeletes_RemovesRegardlessOfUploader()
    {
        var attachment = MakeAttachment(Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(attachment.Id, default)).ReturnsAsync(attachment);

        await _handler.Handle(new DeleteAttachmentCommand(
            attachment.TicketId, attachment.Id,
            Guid.NewGuid(), UserRole.Admin), default);

        _storage.Verify(s => s.DeleteAsync("tickets/file.png", default), Times.Once);
        _repo.Verify(r => r.RemoveAsync(attachment, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentDeletesOwnAttachment_Removes()
    {
        var agentId = Guid.NewGuid();
        var attachment = MakeAttachment(agentId);
        _repo.Setup(r => r.FindByIdAsync(attachment.Id, default)).ReturnsAsync(attachment);

        await _handler.Handle(new DeleteAttachmentCommand(
            attachment.TicketId, attachment.Id, agentId, UserRole.Agent), default);

        _repo.Verify(r => r.RemoveAsync(attachment, default), Times.Once);
    }

    [Fact]
    public async Task Handle_AgentDeletesOtherAgentAttachment_ThrowsUnauthorized()
    {
        var attachment = MakeAttachment(Guid.NewGuid()); // Different agent uploaded it
        _repo.Setup(r => r.FindByIdAsync(attachment.Id, default)).ReturnsAsync(attachment);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new DeleteAttachmentCommand(
                attachment.TicketId, attachment.Id,
                Guid.NewGuid(), UserRole.Agent), default));
    }

    [Fact]
    public async Task Handle_AttachmentNotFound_ThrowsKeyNotFoundException()
    {
        _repo.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), default))
             .ReturnsAsync((Attachment?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new DeleteAttachmentCommand(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), UserRole.Agent), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeleteAttachmentCommandHandlerTests" -v n
```

Expected: FAIL — `DeleteAttachmentCommand` does not exist yet.

- [ ] **Step 3: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/DeleteAttachmentCommand.cs
using CRM.Application.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record DeleteAttachmentCommand(
    Guid TicketId,
    Guid AttachmentId,
    Guid RequestingUserId,
    UserRole RequestingUserRole) : IRequest;

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IAttachmentRepository _attachments;
    private readonly IStorageService _storage;

    public DeleteAttachmentCommandHandler(
        IAttachmentRepository attachments, IStorageService storage)
    {
        _attachments = attachments;
        _storage = storage;
    }

    public async Task Handle(DeleteAttachmentCommand cmd, CancellationToken ct)
    {
        var attachment = await _attachments.FindByIdAsync(cmd.AttachmentId, ct)
            ?? throw new KeyNotFoundException($"Attachment {cmd.AttachmentId} not found.");

        bool isPrivileged = cmd.RequestingUserRole is UserRole.Admin or UserRole.Manager;
        bool isUploader = attachment.UploadedByUserId == cmd.RequestingUserId;

        if (!isPrivileged && !isUploader)
            throw new UnauthorizedAccessException("Only the uploader, managers, or admins can delete attachments.");

        await _storage.DeleteAsync(attachment.StorageKey, ct);
        await _attachments.RemoveAsync(attachment, ct);
        await _attachments.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "DeleteAttachmentCommandHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/DeleteAttachmentCommand.cs \
        tests/CRM.Application.Tests/Tickets/DeleteAttachmentCommandHandlerTests.cs
git commit -m "feat(tickets): add DeleteAttachmentCommand with ownership check"
```

---

## Task 2: TicketsController — DELETE /api/tickets/{id}/attachments/{attachmentId}

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerDeleteAttachmentTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerDeleteAttachmentTests.cs
using System.Net;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerDeleteAttachmentTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task DeleteAttachment_Authorized_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default)).Returns(Task.CompletedTask);
        var client = BuildClient("Admin");

        var response = await client.DeleteAsync(
            $"/api/tickets/{Guid.NewGuid()}/attachments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAttachment_NotOwner_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<IRequest>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Not uploader."));
        var client = BuildClient("Agent");

        var response = await client.DeleteAsync(
            $"/api/tickets/{Guid.NewGuid()}/attachments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerDeleteAttachmentTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add DeleteAttachment endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:
// (Also add using CRM.Domain.Users;)

[HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
public async Task<IActionResult> DeleteAttachment(
    Guid id, Guid attachmentId, CancellationToken ct)
{
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Agent";
    Enum.TryParse<UserRole>(roleClaim, out var role);

    try
    {
        await _mediator.Send(
            new DeleteAttachmentCommand(id, attachmentId, CurrentUserId, role), ct);
        return NoContent();
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex) { return StatusCode(403, new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerDeleteAttachmentTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerDeleteAttachmentTests.cs
git commit -m "feat(api): add DELETE /api/tickets/{id}/attachments/{attachmentId} endpoint"
```
