using CRM.Domain.Surveys;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class CsatSurveyRepository : ICsatSurveyRepository
{
    private readonly AppDbContext _db;
    public CsatSurveyRepository(AppDbContext db) => _db = db;

    public Task<CsatSurvey?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.CsatSurveys.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> ExistsForTicketAsync(Guid ticketId, CancellationToken ct = default)
        => _db.CsatSurveys.AnyAsync(s => s.TicketId == ticketId, ct);

    public async Task AddAsync(CsatSurvey survey, CancellationToken ct = default)
        => await _db.CsatSurveys.AddAsync(survey, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<CsatSurvey>> ListExpiredAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        return await _db.CsatSurveys
            .Where(s => s.Status == "Sent" && s.SentAt < cutoff)
            .ToListAsync(ct);
    }
}
