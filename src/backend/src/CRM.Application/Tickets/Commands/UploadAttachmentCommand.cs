using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Customers;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
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
    private readonly IUserRepository _users;
    private readonly ICustomerRepository _customers;

    public UploadAttachmentCommandHandler(
        ITicketRepository tickets,
        IAttachmentRepository attachments,
        IStorageService storage,
        IUserRepository users,
        ICustomerRepository customers)
    {
        _tickets = tickets;
        _attachments = attachments;
        _storage = storage;
        _users = users;
        _customers = customers;
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

        string? uploaderName = null;
        var user = await _users.FindByIdAsync(cmd.UploadedByUserId, ct);
        if (user is not null)
            uploaderName = $"{user.FirstName} {user.LastName}";
        else
        {
            var customer = await _customers.FindByIdAsync(cmd.UploadedByUserId, ct);
            uploaderName = customer?.FullName;
        }

        return new AttachmentDto(
            attachment.Id, attachment.TicketId, null,
            attachment.FileName, attachment.ContentType, attachment.FileSize,
            presignedUrl, uploaderName, attachment.UploadedAt);
    }
}
