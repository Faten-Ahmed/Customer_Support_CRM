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
                new ValidationFailure("Status", "Survey already submitted.")
                    { ErrorCode = "SURVEY_ALREADY_SUBMITTED" }
            });

        if (survey.IsExpired)
            throw new ValidationException(new[]
            {
                new ValidationFailure("Status", "Survey has expired.")
                    { ErrorCode = "SURVEY_EXPIRED" }
            });

        if (cmd.Rating is < 1 or > 5)
            throw new ValidationException(new[]
            {
                new ValidationFailure("Rating", "Rating must be between 1 and 5.")
                    { ErrorCode = "INVALID_RATING" }
            });

        survey.Submit(cmd.Rating, cmd.Comment);
        await _surveys.SaveChangesAsync(ct);

        await _publisher.Publish(
            new CsatSubmittedEvent(survey.Id, survey.DepartmentId, cmd.Rating), ct);
    }
}
