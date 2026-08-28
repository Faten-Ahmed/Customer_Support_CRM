# Portal Survey Get & Submit — Implementation Plan

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

**Story:** US-BE-082  
**Goal:** Implement `GET /api/portal/surveys/{id}` and `POST /api/portal/surveys/{id}/submit`. GET returns survey with ticket context; 403 if another customer's survey. POST validates: rating 1–5 (422 `INVALID_RATING`), not expired (422 `SURVEY_EXPIRED`), not already submitted (422 `SURVEY_ALREADY_SUBMITTED`). Publishes `CsatSubmittedEvent` on success.

**Architecture:** `GetPortalSurveyQuery(SurveyId, CustomerId)` and `SubmitPortalSurveyCommand(SurveyId, CustomerId, Rating, Comment?)`. Uses `ICsatSurveyRepository`. Adds endpoints to `PortalController`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Surveys/CsatSurvey.cs` |
| Create | `src/CRM.Domain/Surveys/ICsatSurveyRepository.cs` |
| Create | `src/CRM.Application/Portal/DTOs/PortalSurveyDto.cs` |
| Create | `src/CRM.Application/Portal/Queries/GetPortalSurveyQuery.cs` |
| Create | `src/CRM.Application/Portal/Commands/SubmitPortalSurveyCommand.cs` |
| Modify | `src/CRM.API/Controllers/PortalController.cs` |
| Test   | `tests/CRM.Application.Tests/Portal/PortalSurveyTests.cs` |

---

## Task 1: Survey Get and Submit

> Note: `PortalController` is from US-BE-080. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Portal/PortalSurveyTests.cs
using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using CRM.Domain.Surveys;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal;

public class PortalSurveyTests
{
    private readonly Mock<ICsatSurveyRepository> _repo = new();
    private readonly Mock<IPublisher> _publisher = new();
    private readonly GetPortalSurveyQueryHandler _getHandler;
    private readonly SubmitPortalSurveyCommandHandler _submitHandler;

    public PortalSurveyTests()
    {
        _getHandler = new GetPortalSurveyQueryHandler(_repo.Object);
        _submitHandler = new SubmitPortalSurveyCommandHandler(_repo.Object, _publisher.Object);
    }

    [Fact]
    public async Task Get_OwnSurvey_ReturnsDto()
    {
        var customerId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var survey = CsatSurvey.Create(ticketId, customerId, Guid.NewGuid(), Guid.NewGuid(),
            "TKT-001", "Need help with login");
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        var result = await _getHandler.Handle(
            new GetPortalSurveyQuery(survey.Id, customerId), default);

        Assert.Equal("TKT-001", result.TicketNumber);
        Assert.False(result.IsExpired);
    }

    [Fact]
    public async Task Get_OtherCustomerSurvey_ThrowsUnauthorizedAccessException()
    {
        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        var survey = CsatSurvey.Create(Guid.NewGuid(), otherCustomerId, Guid.NewGuid(), Guid.NewGuid(),
            "TKT-002", "Another issue");
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _getHandler.Handle(new GetPortalSurveyQuery(survey.Id, customerId), default));
    }

    [Fact]
    public async Task Submit_ValidRating_SubmitsSurveyAndPublishesEvent()
    {
        var customerId = Guid.NewGuid();
        var survey = CsatSurvey.Create(Guid.NewGuid(), customerId, Guid.NewGuid(), Guid.NewGuid(),
            "TKT-003", "Issue");
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        await _submitHandler.Handle(
            new SubmitPortalSurveyCommand(survey.Id, customerId, 5, "Excellent!"),
            default);

        Assert.Equal("Submitted", survey.Status);
        Assert.Equal(5, survey.Rating);
        _publisher.Verify(p => p.Publish(
            It.Is<CsatSubmittedEvent>(e => e.SurveyId == survey.Id),
            default), Times.Once);
    }

    [Fact]
    public async Task Submit_RatingOutOfRange_ThrowsValidationException()
    {
        var customerId = Guid.NewGuid();
        var survey = CsatSurvey.Create(Guid.NewGuid(), customerId, Guid.NewGuid(), Guid.NewGuid(),
            "TKT-004", "Issue");
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _submitHandler.Handle(
                new SubmitPortalSurveyCommand(survey.Id, customerId, 6, null), default));

        Assert.Contains("INVALID_RATING", ex.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task Submit_ExpiredSurvey_ThrowsValidationException()
    {
        var customerId = Guid.NewGuid();
        var survey = CsatSurvey.CreateExpired(Guid.NewGuid(), customerId, Guid.NewGuid(), Guid.NewGuid());
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _submitHandler.Handle(
                new SubmitPortalSurveyCommand(survey.Id, customerId, 4, null), default));

        Assert.Contains("SURVEY_EXPIRED", ex.Errors.First().ErrorCode);
    }

    [Fact]
    public async Task Submit_AlreadySubmitted_ThrowsValidationException()
    {
        var customerId = Guid.NewGuid();
        var survey = CsatSurvey.Create(Guid.NewGuid(), customerId, Guid.NewGuid(), Guid.NewGuid(),
            "TKT-005", "Issue");
        survey.Submit(5, null);
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _submitHandler.Handle(
                new SubmitPortalSurveyCommand(survey.Id, customerId, 3, null), default));

        Assert.Contains("SURVEY_ALREADY_SUBMITTED", ex.Errors.First().ErrorCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PortalSurveyTests" -v n
```

Expected: FAIL — `CsatSurvey` entity does not exist yet.

- [ ] **Step 3: Create CsatSurvey entity**

```csharp
// src/CRM.Domain/Surveys/CsatSurvey.cs
namespace CRM.Domain.Surveys;

public class CsatSurvey
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid AgentId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string TicketNumber { get; private set; } = string.Empty;
    public string TicketSubject { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Sent";   // Sent | Submitted | Expired
    public int? Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public bool IsExpired => Status == "Expired" || DateTime.UtcNow > ExpiresAt;

    private CsatSurvey() { }

    public static CsatSurvey Create(
        Guid ticketId, Guid customerId, Guid agentId, Guid departmentId,
        string ticketNumber, string ticketSubject) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        CustomerId = customerId,
        AgentId = agentId,
        DepartmentId = departmentId,
        TicketNumber = ticketNumber,
        TicketSubject = ticketSubject,
        Status = "Sent",
        SentAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7)
    };

    public static CsatSurvey CreateExpired(
        Guid ticketId, Guid customerId, Guid agentId, Guid departmentId) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        CustomerId = customerId,
        AgentId = agentId,
        DepartmentId = departmentId,
        TicketNumber = "TKT-EXP",
        TicketSubject = "Expired",
        Status = "Expired",
        SentAt = DateTime.UtcNow.AddDays(-8),
        ExpiresAt = DateTime.UtcNow.AddDays(-1)
    };

    public void Submit(int rating, string? comment)
    {
        Rating = rating;
        Comment = comment;
        Status = "Submitted";
        SubmittedAt = DateTime.UtcNow;
    }

    public void Expire() => Status = "Expired";
}
```

- [ ] **Step 4: Create ICsatSurveyRepository and CsatSubmittedEvent**

```csharp
// src/CRM.Domain/Surveys/ICsatSurveyRepository.cs
namespace CRM.Domain.Surveys;

public interface ICsatSurveyRepository
{
    Task<CsatSurvey?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsForTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task AddAsync(CsatSurvey survey, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CsatSurvey>> ListExpiredAsync(CancellationToken ct = default);
}
```

```csharp
// src/CRM.Domain/Surveys/Events/CsatSubmittedEvent.cs
using MediatR;
namespace CRM.Domain.Surveys.Events;
public record CsatSubmittedEvent(Guid SurveyId, Guid DepartmentId, int Rating) : INotification;
```

- [ ] **Step 5: Create PortalSurveyDto**

```csharp
// src/CRM.Application/Portal/DTOs/PortalSurveyDto.cs
namespace CRM.Application.Portal.DTOs;

public record PortalSurveyDto(
    Guid Id,
    string TicketNumber,
    string TicketSubject,
    DateTime SentAt,
    bool IsExpired,
    string Status);
```

- [ ] **Step 6: Implement GetPortalSurveyQuery**

```csharp
// src/CRM.Application/Portal/Queries/GetPortalSurveyQuery.cs
using CRM.Application.Portal.DTOs;
using CRM.Domain.Surveys;
using MediatR;

namespace CRM.Application.Portal.Queries;

public record GetPortalSurveyQuery(Guid SurveyId, Guid CustomerId) : IRequest<PortalSurveyDto>;

public class GetPortalSurveyQueryHandler
    : IRequestHandler<GetPortalSurveyQuery, PortalSurveyDto>
{
    private readonly ICsatSurveyRepository _surveys;
    public GetPortalSurveyQueryHandler(ICsatSurveyRepository surveys) => _surveys = surveys;

    public async Task<PortalSurveyDto> Handle(GetPortalSurveyQuery query, CancellationToken ct)
    {
        var survey = await _surveys.FindByIdAsync(query.SurveyId, ct)
            ?? throw new KeyNotFoundException($"Survey {query.SurveyId} not found.");

        if (survey.CustomerId != query.CustomerId)
            throw new UnauthorizedAccessException("You can only view your own surveys.");

        return new PortalSurveyDto(
            survey.Id, survey.TicketNumber, survey.TicketSubject,
            survey.SentAt, survey.IsExpired, survey.Status);
    }
}
```

- [ ] **Step 7: Implement SubmitPortalSurveyCommand**

```csharp
// src/CRM.Application/Portal/Commands/SubmitPortalSurveyCommand.cs
using CRM.Domain.Surveys;
using CRM.Domain.Surveys.Events;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CRM.Application.Portal.Commands;

public record SubmitPortalSurveyCommand(
    Guid SurveyId, Guid CustomerId, int Rating, string? Comment) : IRequest;

public class SubmitPortalSurveyCommandHandler : IRequestHandler<SubmitPortalSurveyCommand>
{
    private readonly ICsatSurveyRepository _surveys;
    private readonly IPublisher _publisher;

    public SubmitPortalSurveyCommandHandler(
        ICsatSurveyRepository surveys, IPublisher publisher)
    {
        _surveys = surveys;
        _publisher = publisher;
    }

    public async Task Handle(SubmitPortalSurveyCommand cmd, CancellationToken ct)
    {
        var survey = await _surveys.FindByIdAsync(cmd.SurveyId, ct)
            ?? throw new KeyNotFoundException($"Survey {cmd.SurveyId} not found.");

        if (survey.CustomerId != cmd.CustomerId)
            throw new UnauthorizedAccessException("You can only submit your own surveys.");

        if (survey.Status == "Submitted")
            throw new ValidationException(new[]
            {
                new ValidationFailure("Status", "Survey already submitted.", "SURVEY_ALREADY_SUBMITTED")
            });

        if (survey.IsExpired)
            throw new ValidationException(new[]
            {
                new ValidationFailure("Status", "Survey has expired.", "SURVEY_EXPIRED")
            });

        if (cmd.Rating is < 1 or > 5)
            throw new ValidationException(new[]
            {
                new ValidationFailure("Rating", "Rating must be between 1 and 5.", "INVALID_RATING")
            });

        survey.Submit(cmd.Rating, cmd.Comment);
        await _surveys.SaveChangesAsync(ct);

        await _publisher.Publish(
            new CsatSubmittedEvent(survey.Id, survey.DepartmentId, cmd.Rating), ct);
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PortalSurveyTests" -v n
```

Expected: 6 tests PASS.

- [ ] **Step 9: Add survey actions to PortalController**

Open `src/CRM.API/Controllers/PortalController.cs` and add:

```csharp
[HttpGet("surveys/{id:guid}")]
public async Task<IActionResult> GetSurvey(Guid id, CancellationToken ct)
{
    try
    {
        var result = await _mediator.Send(new GetPortalSurveyQuery(id, CurrentCustomerId), ct);
        return Ok(new { data = result });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex)
        { return StatusCode(403, new { error = ex.Message }); }
}

[HttpPost("surveys/{id:guid}/submit")]
public async Task<IActionResult> SubmitSurvey(
    Guid id, [FromBody] SurveySubmitRequest req, CancellationToken ct)
{
    try
    {
        await _mediator.Send(
            new SubmitPortalSurveyCommand(id, CurrentCustomerId, req.Rating, req.Comment), ct);
        return Ok(new { message = "Survey submitted successfully." });
    }
    catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    catch (UnauthorizedAccessException ex)
        { return StatusCode(403, new { error = ex.Message }); }
    catch (FluentValidation.ValidationException ex)
        { return UnprocessableEntity(new { error = ex.Errors.First().ErrorCode }); }
}
```

Add the request record at the bottom of the file:

```csharp
public record SurveySubmitRequest(int Rating, string? Comment);
```

- [ ] **Step 10: Commit**

```bash
git add src/CRM.Domain/Surveys/ \
        src/CRM.Application/Portal/DTOs/PortalSurveyDto.cs \
        src/CRM.Application/Portal/Queries/GetPortalSurveyQuery.cs \
        src/CRM.Application/Portal/Commands/SubmitPortalSurveyCommand.cs \
        src/CRM.API/Controllers/PortalController.cs \
        tests/CRM.Application.Tests/Portal/PortalSurveyTests.cs
git commit -m "feat(portal): add GET /portal/surveys/{id} and POST /portal/surveys/{id}/submit with CSAT event"
```
