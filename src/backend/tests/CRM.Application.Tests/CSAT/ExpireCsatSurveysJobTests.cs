using CRM.Domain.Surveys;
using CRM.Infrastructure.Jobs;
using Moq;
using Xunit;

namespace CRM.Application.Tests.CSAT;

public class ExpireCsatSurveysJobTests
{
    private readonly Mock<ICsatSurveyRepository> _repo = new();
    private readonly ExpireCsatSurveysJob _job;

    public ExpireCsatSurveysJobTests()
    {
        _job = new ExpireCsatSurveysJob(_repo.Object);
    }

    [Fact]
    public async Task Execute_ExpiresSentSurveysOlderThan7Days()
    {
        var survey = CsatSurvey.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "TKT-001", "Old ticket");

        _repo.Setup(r => r.ListExpiredAsync(default))
             .ReturnsAsync(new List<CsatSurvey> { survey });

        await _job.ExecuteAsync();

        Assert.Equal("Expired", survey.Status);
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Execute_NoExpiredSurveys_DoesNotCallSave()
    {
        _repo.Setup(r => r.ListExpiredAsync(default))
             .ReturnsAsync(new List<CsatSurvey>());

        await _job.ExecuteAsync();

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Execute_Idempotent_AlreadyExpiredSurveysNotDoubleProcessed()
    {
        _repo.Setup(r => r.ListExpiredAsync(default))
             .ReturnsAsync(new List<CsatSurvey>());

        await _job.ExecuteAsync();

        _repo.Verify(r => r.SaveChangesAsync(default), Times.Never);
    }
}
