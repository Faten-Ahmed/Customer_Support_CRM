using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using CRM.Domain.Surveys;
using CRM.Domain.Surveys.Events;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

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
        var survey = CsatSurvey.Create(Guid.NewGuid(), customerId, Guid.NewGuid(), Guid.NewGuid(),
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
        var survey = CsatSurvey.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "TKT-002", "Another issue");
        _repo.Setup(r => r.FindByIdAsync(survey.Id, default)).ReturnsAsync(survey);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _getHandler.Handle(new GetPortalSurveyQuery(survey.Id, Guid.NewGuid()), default));
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
