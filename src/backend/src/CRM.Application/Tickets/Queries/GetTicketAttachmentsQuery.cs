using CRM.Application.Common;
using CRM.Application.Tickets.DTOs;
using CRM.Domain.Tickets;
using MediatR;

namespace CRM.Application.Tickets.Queries;

public record GetTicketAttachmentsQuery(Guid TicketId) : IRequest<List<AttachmentDto>>;

public class GetTicketAttachmentsQueryHandler
    : IRequestHandler<GetTicketAttachmentsQuery, List<AttachmentDto>>
{
    private readonly IAttachmentRepository _attachments;
    private readonly IStorageService _storage;

    public GetTicketAttachmentsQueryHandler(
        IAttachmentRepository attachments,
        IStorageService storage)
    {
        _attachments = attachments;
        _storage = storage;
    }

    public async Task<List<AttachmentDto>> Handle(
        GetTicketAttachmentsQuery query, CancellationToken ct)
    {
        var projections = await _attachments.ListByTicketAsync(query.TicketId, ct);

        var dtos = new List<AttachmentDto>(projections.Count);
        foreach (var p in projections)
        {
            var url = await _storage.GetPresignedUrlAsync(p.StorageKey, ct);
            dtos.Add(new AttachmentDto(
                p.Id,
                p.TicketId,
                p.MessageId,
                p.FileName,
                p.ContentType,
                p.FileSize,
                url,
                p.UploaderName,
                p.UploadedAt));
        }

        return dtos;
    }
}
