# Upload Attachment — Implementation Plan

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

**Story:** US-BE-030  
**Goal:** Implement `POST /api/tickets/{id}/attachments` — uploads a file to S3/MinIO and creates an `Attachment` record linked to the ticket.

**Architecture:** `UploadAttachmentCommand(ticketId, file, uploadedByUserId)` → handler validates file size (≤10MB) and allowed MIME types, uploads to `IStorageService` (S3/MinIO), creates `Attachment` domain record with S3 key + content type + size, persists. Returns `AttachmentDto` with pre-signed URL.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, AWS SDK / MinIO, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/Attachment.cs` |
| Create | `src/CRM.Domain/Tickets/IAttachmentRepository.cs` |
| Create | `src/CRM.Application/Common/IStorageService.cs` |
| Create | `src/CRM.Application/Tickets/Commands/UploadAttachmentCommand.cs` |
| Create | `src/CRM.Application/Tickets/DTOs/AttachmentDto.cs` |
| Create | `src/CRM.Application/Tickets/Validators/UploadAttachmentCommandValidator.cs` |
| Modify | `src/CRM.API/Controllers/TicketsController.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/UploadAttachmentCommandHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Tickets/TicketsControllerAttachmentTests.cs` |

---

## Task 1: Attachment Domain + IStorageService

**Files:**
- Create: `src/CRM.Domain/Tickets/Attachment.cs`
- Create: `src/CRM.Domain/Tickets/IAttachmentRepository.cs`
- Create: `src/CRM.Application/Common/IStorageService.cs`

- [ ] **Step 1: Create Attachment entity and interfaces**

```csharp
// src/CRM.Domain/Tickets/Attachment.cs
namespace CRM.Domain.Tickets;

public class Attachment
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public Guid UploadedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Attachment() { }

    public static Attachment Create(
        Guid ticketId, string fileName, string contentType,
        long sizeBytes, string storageKey, Guid uploadedBy)
        => new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageKey = storageKey,
            UploadedByUserId = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };
}
```

```csharp
// src/CRM.Domain/Tickets/IAttachmentRepository.cs
namespace CRM.Domain.Tickets;

public interface IAttachmentRepository
{
    Task<Attachment?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Attachment attachment, CancellationToken ct = default);
    Task RemoveAsync(Attachment attachment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

```csharp
// src/CRM.Application/Common/IStorageService.cs
namespace CRM.Application.Common;

public interface IStorageService
{
    Task<string> UploadAsync(
        string key, Stream content, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    string GetPresignedUrl(string key, TimeSpan expiry);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.Domain/Tickets/Attachment.cs \
        src/CRM.Domain/Tickets/IAttachmentRepository.cs \
        src/CRM.Application/Common/IStorageService.cs
git commit -m "feat(domain): add Attachment entity, IAttachmentRepository, IStorageService"
```

---

## Task 2: UploadAttachment Command + Handler + Validator + DTO

**Files:**
- Create: `src/CRM.Application/Tickets/DTOs/AttachmentDto.cs`
- Create: `src/CRM.Application/Tickets/Commands/UploadAttachmentCommand.cs`
- Create: `src/CRM.Application/Tickets/Validators/UploadAttachmentCommandValidator.cs`
- Test: `tests/CRM.Application.Tests/Tickets/UploadAttachmentCommandHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/UploadAttachmentCommandHandlerTests.cs
using CRM.Application.Common;
using CRM.Application.Tickets.Commands;
using CRM.Domain.Tickets;
using CRM.Domain.Tickets.Enums;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class UploadAttachmentCommandHandlerTests
{
    private readonly Mock<ITicketRepository> _ticketRepo = new();
    private readonly Mock<IAttachmentRepository> _attachmentRepo = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly UploadAttachmentCommandHandler _handler;

    public UploadAttachmentCommandHandlerTests()
    {
        _handler = new UploadAttachmentCommandHandler(
            _ticketRepo.Object, _attachmentRepo.Object, _storage.Object);
    }

    [Fact]
    public async Task Handle_ValidFile_UploadsAndReturnsDto()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());

        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);
        _storage.Setup(s => s.UploadAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), "image/png", default))
            .ReturnsAsync("tickets/file.png");
        _storage.Setup(s => s.GetPresignedUrl("tickets/file.png", It.IsAny<TimeSpan>()))
                .Returns("https://s3.example.com/file.png?sig=xxx");

        var stream = new MemoryStream(new byte[1024]);
        var result = await _handler.Handle(new UploadAttachmentCommand(
            ticketId, "screenshot.png", "image/png", stream.Length, stream, Guid.NewGuid()), default);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("screenshot.png", result.FileName);
        Assert.Contains("s3.example.com", result.DownloadUrl);
        _attachmentRepo.Verify(r => r.AddAsync(It.IsAny<Attachment>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        var oversizedStream = new MemoryStream(new byte[11 * 1024 * 1024]); // 11MB

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UploadAttachmentCommand(
                ticketId, "big.mp4", "video/mp4",
                oversizedStream.Length, oversizedStream, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_DisallowedMimeType_ThrowsInvalidOperationException()
    {
        var ticketId = Guid.NewGuid();
        var ticket = Ticket.Create(Guid.NewGuid(), "S", "D",
            TicketPriority.Low, TicketChannel.Internal, Guid.NewGuid());
        _ticketRepo.Setup(r => r.FindByIdAsync(ticketId, default)).ReturnsAsync(ticket);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new UploadAttachmentCommand(
                ticketId, "virus.exe", "application/x-msdownload",
                1024, new MemoryStream(new byte[1024]), Guid.NewGuid()), default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UploadAttachmentCommandHandlerTests" -v n
```

Expected: FAIL — `UploadAttachmentCommand` does not exist yet.

- [ ] **Step 3: Create DTO**

```csharp
// src/CRM.Application/Tickets/DTOs/AttachmentDto.cs
namespace CRM.Application.Tickets.DTOs;

public record AttachmentDto(
    Guid Id,
    Guid TicketId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string DownloadUrl,
    DateTime CreatedAt);
```

- [ ] **Step 4: Implement command and handler**

```csharp
// src/CRM.Application/Tickets/Commands/UploadAttachmentCommand.cs
using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record UploadAttachmentCommand(
    Guid TicketId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    Guid UploadedByUserId) : IRequest<AttachmentDto>;

public class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    private static readonly HashSet<string> _allowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain"
    };

    private const long MaxFileSizeBytes = 10L * 1024 * 1024; // 10MB

    private readonly ITicketRepository _tickets;
    private readonly IAttachmentRepository _attachments;
    private readonly IStorageService _storage;

    public UploadAttachmentCommandHandler(
        ITicketRepository tickets,
        IAttachmentRepository attachments,
        IStorageService storage)
    {
        _tickets = tickets;
        _attachments = attachments;
        _storage = storage;
    }

    public async Task<AttachmentDto> Handle(UploadAttachmentCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (cmd.SizeBytes > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds maximum size of 10MB.");

        if (!_allowedMimeTypes.Contains(cmd.ContentType))
            throw new InvalidOperationException($"File type '{cmd.ContentType}' is not allowed.");

        var key = $"tickets/{cmd.TicketId}/{Guid.NewGuid()}/{cmd.FileName}";
        await _storage.UploadAsync(key, cmd.Content, cmd.ContentType, ct);

        var attachment = Attachment.Create(
            cmd.TicketId, cmd.FileName, cmd.ContentType, cmd.SizeBytes, key, cmd.UploadedByUserId);

        await _attachments.AddAsync(attachment, ct);
        await _attachments.SaveChangesAsync(ct);

        var url = _storage.GetPresignedUrl(key, TimeSpan.FromHours(1));

        return new AttachmentDto(
            attachment.Id, attachment.TicketId, attachment.FileName,
            attachment.ContentType, attachment.SizeBytes, url, attachment.CreatedAt);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "UploadAttachmentCommandHandlerTests" -v n
```

Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CRM.Application/Tickets/Commands/UploadAttachmentCommand.cs \
        src/CRM.Application/Tickets/DTOs/AttachmentDto.cs \
        tests/CRM.Application.Tests/Tickets/UploadAttachmentCommandHandlerTests.cs
git commit -m "feat(tickets): add UploadAttachmentCommand with MIME type and size validation"
```

---

## Task 3: TicketsController — POST /api/tickets/{id}/attachments

**Files:**
- Modify: `src/CRM.API/Controllers/TicketsController.cs`
- Test: `tests/CRM.API.Tests/Tickets/TicketsControllerAttachmentTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Tickets/TicketsControllerAttachmentTests.cs
using System.Net;
using System.Net.Http.Json;
using CRM.Application.Tickets.Commands;
using CRM.Application.Tickets.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Tickets;

public class TicketsControllerAttachmentTests
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
    public async Task UploadAttachment_ValidFile_Returns201()
    {
        var ticketId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<UploadAttachmentCommand>(), default))
                 .ReturnsAsync(new AttachmentDto(
                     Guid.NewGuid(), ticketId, "screenshot.png", "image/png",
                     1024, "https://s3.example.com/file.png", DateTime.UtcNow));

        var client = BuildClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(new MemoryStream(new byte[1024])), "file", "screenshot.png");

        var response = await client.PostAsync($"/api/tickets/{ticketId}/attachments", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_DisallowedType_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<UploadAttachmentCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("File type not allowed."));

        var client = BuildClient();
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(new MemoryStream(new byte[512])), "file", "virus.exe");

        var response = await client.PostAsync($"/api/tickets/{Guid.NewGuid()}/attachments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerAttachmentTests" -v n
```

Expected: FAIL — endpoint does not exist.

- [ ] **Step 3: Add UploadAttachment endpoint to TicketsController**

```csharp
// Add to src/CRM.API/Controllers/TicketsController.cs inside the class:

[HttpPost("{id:guid}/attachments")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadAttachment(
    Guid id, IFormFile file, CancellationToken ct)
{
    if (file is null || file.Length == 0)
        return BadRequest(new { error = "No file provided." });

    try
    {
        var result = await _mediator.Send(new UploadAttachmentCommand(
            id, file.FileName, file.ContentType, file.Length,
            file.OpenReadStream(), CurrentUserId), ct);
        return StatusCode(201, result);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "TicketsControllerAttachmentTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Controllers/TicketsController.cs \
        tests/CRM.API.Tests/Tickets/TicketsControllerAttachmentTests.cs
git commit -m "feat(api): add POST /api/tickets/{id}/attachments multipart upload endpoint"
```
