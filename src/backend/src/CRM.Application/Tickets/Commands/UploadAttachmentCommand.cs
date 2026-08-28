using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record UploadAttachmentCommand(
    Guid TicketId,
    string FileName,
    string ContentType,
    long FileSize,
    Stream Content,
    Guid UploadedByUserId) : IRequest<AttachmentDto>;

public class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain"
    };

    private const long MaxFileSizeBytes = 10L * 1024 * 1024;

    private readonly ITicketRepository _tickets;
    private readonly IAttachmentRepository _attachments;
    private readonly IStorageService _storage;

    public UploadAttachmentCommandHandler(
        ITicketRepository tickets,
        IAttachmentRepository attachments,
        IStorageService storage)
    {
        _tickets = tickets;
        _attachments = attachments;
        _storage = storage;
    }

    public async Task<AttachmentDto> Handle(UploadAttachmentCommand cmd, CancellationToken ct)
    {
        var ticket = await _tickets.FindByIdAsync(cmd.TicketId, ct)
            ?? throw new KeyNotFoundException($"Ticket {cmd.TicketId} not found.");

        if (cmd.FileSize > MaxFileSizeBytes)
            throw new InvalidOperationException("File exceeds maximum size of 10MB.");

        if (!AllowedMimeTypes.Contains(cmd.ContentType))
            throw new InvalidOperationException($"File type '{cmd.ContentType}' is not allowed.");

        var storageKey = await _storage.UploadAsync(cmd.Content, cmd.FileName, cmd.ContentType, ct);

        var attachment = Attachment.Create(
            cmd.TicketId, null, cmd.FileName, cmd.ContentType,
            cmd.FileSize, storageKey, cmd.UploadedByUserId);

        await _attachments.AddAsync(attachment, ct);
        await _attachments.SaveChangesAsync(ct);

        var presignedUrl = await _storage.GetPresignedUrlAsync(storageKey, ct);

        return new AttachmentDto(
            attachment.Id, attachment.TicketId, null,
            attachment.FileName, attachment.ContentType, attachment.FileSize,
            presignedUrl, null, attachment.UploadedAt);
    }
}
