using CRM.Application.Common;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using MediatR;

namespace CRM.Application.Tickets.Commands;

public record DeleteAttachmentCommand(
    Guid TicketId,
    Guid AttachmentId,
    Guid RequestingUserId,
    UserRole RequestingUserRole) : IRequest;

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand>
{
    private readonly IAttachmentRepository _attachments;
    private readonly IStorageService _storage;

    public DeleteAttachmentCommandHandler(
        IAttachmentRepository attachments, IStorageService storage)
    {
        _attachments = attachments;
        _storage = storage;
    }

    public async Task Handle(DeleteAttachmentCommand cmd, CancellationToken ct)
    {
        var attachment = await _attachments.FindByIdAsync(cmd.AttachmentId, ct)
            ?? throw new KeyNotFoundException($"Attachment {cmd.AttachmentId} not found.");

        bool isPrivileged = cmd.RequestingUserRole is UserRole.Admin or UserRole.Manager;
        bool isUploader = attachment.UploadedByUserId == cmd.RequestingUserId;

        if (!isPrivileged && !isUploader)
            throw new UnauthorizedAccessException(
                "Only the uploader, managers, or admins can delete attachments.");

        await _storage.DeleteAsync(attachment.StorageKey, ct);
        _attachments.Remove(attachment);
        await _attachments.SaveChangesAsync(ct);
    }
}
