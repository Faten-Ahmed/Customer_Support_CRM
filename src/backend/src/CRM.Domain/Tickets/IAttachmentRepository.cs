namespace CRM.Domain.Tickets;

public record AttachmentProjection(
    Guid Id,
    Guid TicketId,
    Guid? MessageId,
    string FileName,
    string ContentType,
    long FileSize,
    string StorageKey,
    Guid UploadedByUserId,
    string? UploaderName,
    DateTime UploadedAt);

public interface IAttachmentRepository
{
    Task<Attachment?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Attachment attachment, CancellationToken ct = default);
    void Remove(Attachment attachment);
    Task<List<AttachmentProjection>> ListByTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
