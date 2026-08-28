namespace CRM.Domain.Tickets;

public class Attachment
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid? MessageId { get; private set; }
    public string FileName { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private Attachment() { }

    public static Attachment Create(
        Guid ticketId,
        Guid? messageId,
        string fileName,
        string contentType,
        long fileSize,
        string storageKey,
        Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key is required.", nameof(storageKey));

        return new Attachment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            MessageId = messageId,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            StorageKey = storageKey,
            UploadedByUserId = uploadedByUserId,
            UploadedAt = DateTime.UtcNow
        };
    }
}
