# Channel Status Endpoint — Implementation Plan

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

**Story:** US-BE-072  
**Goal:** Implement `GET /api/admin/channels/status` — returns live connectivity status for all 5 inbound/outbound channels (email, whatsapp, sms, liveChat, portal), including SMTP handshake, Twilio credential validation, and LiveChat session counts.

**Architecture:** `GetChannelStatusQuery` → handler injects `IEmailHealthChecker`, `ITwilioHealthChecker`, `ILiveChatSessionRepository`; aggregates results into a 5-channel list. Portal always reports configured. Channel interfaces live in `CRM.Domain.Channels`; implementations in `CRM.Infrastructure`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Channels/IEmailHealthChecker.cs` |
| Create | `src/CRM.Domain/Channels/ITwilioHealthChecker.cs` |
| Create | `src/CRM.Domain/Channels/ILiveChatSessionRepository.cs` |
| Create | `src/CRM.Application/Admin/Channels/DTOs/ChannelStatusDto.cs` |
| Create | `src/CRM.Application/Admin/Channels/Queries/GetChannelStatusQuery.cs` |
| Create | `src/CRM.API/Controllers/AdminChannelsController.cs` |
| Test   | `tests/CRM.Application.Tests/Admin/GetChannelStatusQueryHandlerTests.cs` |
| Test   | `tests/CRM.API.Tests/Admin/AdminChannelsControllerTests.cs` |

---

## Task 1: Channel Status

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Admin/GetChannelStatusQueryHandlerTests.cs
using CRM.Application.Admin.Channels.Queries;
using CRM.Domain.Channels;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Admin;

public class GetChannelStatusQueryHandlerTests
{
    private readonly Mock<IEmailHealthChecker> _email = new();
    private readonly Mock<ITwilioHealthChecker> _twilio = new();
    private readonly Mock<ILiveChatSessionRepository> _liveChat = new();
    private readonly GetChannelStatusQueryHandler _handler;

    public GetChannelStatusQueryHandlerTests()
    {
        _handler = new GetChannelStatusQueryHandler(
            _email.Object, _twilio.Object, _liveChat.Object);
    }

    [Fact]
    public async Task Handle_AllChannelsHealthy_Returns5Channels()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(true, DateTime.UtcNow.AddHours(-1), null));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(true, null));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(3, 1));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        Assert.Equal(5, result.Channels.Count);
        Assert.Contains(result.Channels, c => c.Channel == "email" && c.Configured);
        Assert.Contains(result.Channels, c => c.Channel == "portal" && c.Configured);
    }

    [Fact]
    public async Task Handle_SmtpDown_ReturnsEmailDisconnected()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(false, null, "Connection refused"));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(true, null));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(0, 0));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        var email = result.Channels.Single(c => c.Channel == "email");
        Assert.False(email.Connected);
        Assert.Equal("Connection refused", email.Error);
    }

    [Fact]
    public async Task Handle_TwilioInvalid_ReturnsWhatsAppAndSmsDisconnected()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(true, null, null));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(false, "Invalid credentials"));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(0, 0));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        var wa = result.Channels.Single(c => c.Channel == "whatsapp");
        var sms = result.Channels.Single(c => c.Channel == "sms");
        Assert.False(wa.Connected);
        Assert.False(sms.Connected);
    }

    [Fact]
    public async Task Handle_LiveChat_ReturnsSessionCounts()
    {
        _email.Setup(e => e.CheckAsync(default))
              .ReturnsAsync(new EmailHealthResult(true, null, null));
        _twilio.Setup(t => t.CheckAsync(default))
               .ReturnsAsync(new TwilioHealthResult(true, null));
        _liveChat.Setup(l => l.GetStatsAsync(default))
                 .ReturnsAsync(new LiveChatStats(ActiveSessions: 5, PendingHandoffs: 2));

        var result = await _handler.Handle(new GetChannelStatusQuery(), default);

        var chat = result.Channels.Single(c => c.Channel == "liveChat");
        Assert.Equal(5, chat.ActiveSessions);
        Assert.Equal(2, chat.PendingHandoffs);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetChannelStatusQueryHandlerTests" -v n
```

Expected: FAIL — `GetChannelStatusQuery` does not exist yet.

- [ ] **Step 3: Create domain channel health interfaces**

```csharp
// src/CRM.Domain/Channels/IEmailHealthChecker.cs
namespace CRM.Domain.Channels;

public record EmailHealthResult(bool Connected, DateTime? LastMessageAt, string? Error);

public interface IEmailHealthChecker
{
    Task<EmailHealthResult> CheckAsync(CancellationToken ct = default);
}
```

```csharp
// src/CRM.Domain/Channels/ITwilioHealthChecker.cs
namespace CRM.Domain.Channels;

public record TwilioHealthResult(bool Valid, string? Error);

public interface ITwilioHealthChecker
{
    Task<TwilioHealthResult> CheckAsync(CancellationToken ct = default);
}
```

```csharp
// src/CRM.Domain/Channels/ILiveChatSessionRepository.cs
namespace CRM.Domain.Channels;

public record LiveChatStats(int ActiveSessions, int PendingHandoffs);

public interface ILiveChatSessionRepository
{
    Task<LiveChatStats> GetStatsAsync(CancellationToken ct = default);
}
```

- [ ] **Step 4: Create ChannelStatusDto**

```csharp
// src/CRM.Application/Admin/Channels/DTOs/ChannelStatusDto.cs
namespace CRM.Application.Admin.Channels.DTOs;

public record ChannelStatusDto(
    string Channel,
    bool Configured,
    bool Connected,
    DateTime? LastMessageAt,
    int? ActiveSessions,
    int? PendingHandoffs,
    string? Error);

public record ChannelStatusListDto(IReadOnlyList<ChannelStatusDto> Channels);
```

- [ ] **Step 5: Implement GetChannelStatusQuery**

```csharp
// src/CRM.Application/Admin/Channels/Queries/GetChannelStatusQuery.cs
using CRM.Application.Admin.Channels.DTOs;
using CRM.Domain.Channels;
using MediatR;

namespace CRM.Application.Admin.Channels.Queries;

public record GetChannelStatusQuery : IRequest<ChannelStatusListDto>;

public class GetChannelStatusQueryHandler
    : IRequestHandler<GetChannelStatusQuery, ChannelStatusListDto>
{
    private readonly IEmailHealthChecker _email;
    private readonly ITwilioHealthChecker _twilio;
    private readonly ILiveChatSessionRepository _liveChat;

    public GetChannelStatusQueryHandler(
        IEmailHealthChecker email,
        ITwilioHealthChecker twilio,
        ILiveChatSessionRepository liveChat)
    {
        _email = email;
        _twilio = twilio;
        _liveChat = liveChat;
    }

    public async Task<ChannelStatusListDto> Handle(
        GetChannelStatusQuery query, CancellationToken ct)
    {
        var (emailResult, twilioResult, liveChatStats) = await (
            _email.CheckAsync(ct),
            _twilio.CheckAsync(ct),
            _liveChat.GetStatsAsync(ct)).WhenAll();

        var channels = new List<ChannelStatusDto>
        {
            new("email", true, emailResult.Connected,
                emailResult.LastMessageAt, null, null, emailResult.Error),

            new("whatsapp", true, twilioResult.Valid,
                null, null, null, twilioResult.Valid ? null : twilioResult.Error),

            new("sms", true, twilioResult.Valid,
                null, null, null, twilioResult.Valid ? null : twilioResult.Error),

            new("liveChat", true, true,
                null, liveChatStats.ActiveSessions, liveChatStats.PendingHandoffs, null),

            new("portal", true, true, null, null, null, null)
        };

        return new ChannelStatusListDto(channels);
    }
}

file static class TaskExtensions
{
    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(
        this (Task<T1>, Task<T2>, Task<T3>) tasks)
    {
        await Task.WhenAll(tasks.Item1, tasks.Item2, tasks.Item3);
        return (tasks.Item1.Result, tasks.Item2.Result, tasks.Item3.Result);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "GetChannelStatusQueryHandlerTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 7: Create AdminChannelsController**

```csharp
// src/CRM.API/Controllers/AdminChannelsController.cs
using CRM.Application.Admin.Channels.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/admin/channels")]
[Authorize(Roles = "Admin")]
public class AdminChannelsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminChannelsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)
        => Ok(new { data = (await _mediator.Send(new GetChannelStatusQuery(), ct)).Channels });
}
```

- [ ] **Step 8: Write controller test**

```csharp
// tests/CRM.API.Tests/Admin/AdminChannelsControllerTests.cs
using System.Net;
using CRM.Application.Admin.Channels.DTOs;
using CRM.Application.Admin.Channels.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminChannelsControllerTests
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
    public async Task Status_Returns200WithFiveChannels()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetChannelStatusQuery>(), default))
                 .ReturnsAsync(new ChannelStatusListDto(new List<ChannelStatusDto>
                 {
                     new("email", true, true, null, null, null, null),
                     new("whatsapp", true, true, null, null, null, null),
                     new("sms", true, true, null, null, null, null),
                     new("liveChat", true, true, null, 2, 0, null),
                     new("portal", true, true, null, null, null, null),
                 }));

        var response = await BuildClient().GetAsync("/api/admin/channels/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Status_NonAdmin_Returns403()
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

        var response = await client.GetAsync("/api/admin/channels/status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 9: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "AdminChannelsControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Domain/Channels/ \
        src/CRM.Application/Admin/Channels/ \
        src/CRM.API/Controllers/AdminChannelsController.cs \
        tests/CRM.Application.Tests/Admin/GetChannelStatusQueryHandlerTests.cs \
        tests/CRM.API.Tests/Admin/AdminChannelsControllerTests.cs
git commit -m "feat(admin): add GET /api/admin/channels/status — live channel health for email, WhatsApp, SMS, LiveChat, Portal"
```
