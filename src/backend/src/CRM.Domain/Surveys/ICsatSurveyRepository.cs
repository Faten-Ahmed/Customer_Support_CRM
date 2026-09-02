namespace CRM.Domain.Surveys;

public interface ICsatSurveyRepository
{
    Task<CsatSurvey?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsForTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task AddAsync(CsatSurvey survey, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CsatSurvey>> ListExpiredAsync(CancellationToken ct = default);
}
