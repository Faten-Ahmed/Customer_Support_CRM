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
