using CRM.Domain.Surveys;

namespace CRM.Infrastructure.Jobs;

public class ExpireCsatSurveysJob
{
    private readonly ICsatSurveyRepository _surveys;

    public ExpireCsatSurveysJob(ICsatSurveyRepository surveys) => _surveys = surveys;

    public async Task ExecuteAsync()
    {
        var expiring = await _surveys.ListExpiredAsync();
        if (expiring.Count == 0) return;

        foreach (var survey in expiring)
            survey.Expire();

        await _surveys.SaveChangesAsync();
    }
}
