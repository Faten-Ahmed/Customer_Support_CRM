using MediatR;
namespace CRM.Domain.Surveys.Events;
public record CsatSubmittedEvent(Guid SurveyId, Guid DepartmentId, int Rating) : INotification;
