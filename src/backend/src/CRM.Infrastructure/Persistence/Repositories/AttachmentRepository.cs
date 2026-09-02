using CRM.Domain.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructure.Persistence.Repositories;

public class AttachmentRepository : IAttachmentRepository
{
    private readonly AppDbContext _db;
    public AttachmentRepository(AppDbContext db) => _db = db;

    public Task<Attachment?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(Attachment attachment, CancellationToken ct = default)
        => await _db.Attachments.AddAsync(attachment, ct);

    public void Remove(Attachment attachment)
        => _db.Attachments.Remove(attachment);

    public async Task<List<AttachmentProjection>> ListByTicketAsync(
        Guid ticketId, CancellationToken ct = default)
    {
        return await _db.Attachments
            .Where(a => a.TicketId == ticketId)
            .OrderBy(a => a.UploadedAt)
            .Select(a => new AttachmentProjection(
                a.Id,
                a.TicketId,
                a.MessageId,
                a.FileName,
                a.ContentType,
                a.FileSize,
                a.StorageKey,
                a.UploadedByUserId,
                _db.Users
                    .Where(u => u.Id == a.UploadedByUserId)
                    .Select(u => u.FirstName + " " + u.LastName)
                    .FirstOrDefault()
                    ?? _db.Customers
                        .Where(c => c.Id == a.UploadedByUserId)
                        .Select(c => c.FullName)
                        .FirstOrDefault(),
                a.UploadedAt))
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
