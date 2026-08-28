using CRM.Domain.Tickets;

namespace CRM.Infrastructure.Persistence.Repositories;

// Stub — real implementation follows in BE infrastructure tasks.
public class AttachmentRepository : IAttachmentRepository
{
    public Task<Attachment?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult<Attachment?>(null);

    public Task AddAsync(Attachment attachment, CancellationToken ct = default)
        => Task.CompletedTask;

    public void Remove(Attachment attachment) { }

    public Task<List<AttachmentProjection>> ListByTicketAsync(Guid ticketId, CancellationToken ct = default)
        => Task.FromResult(new List<AttachmentProjection>());

    public Task SaveChangesAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
