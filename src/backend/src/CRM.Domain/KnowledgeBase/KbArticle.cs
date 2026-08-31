namespace CRM.Domain.KnowledgeBase;

public enum KbArticleStatus { Draft, PendingReview, Published, Archived }
public enum KbVisibility { Internal, Public, Both }

public class KbArticle
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string? TitleAr { get; private set; }
    public string? Content { get; private set; }
    public string? ContentAr { get; private set; }
    public Guid CategoryId { get; private set; }
    public KbArticleStatus Status { get; private set; }
    public KbVisibility Visibility { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public string? RejectionNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private KbArticle() { }

    public static KbArticle Create(
        Guid categoryId, string title, Guid createdByUserId,
        KbVisibility visibility = KbVisibility.Internal,
        string? content = null, string? titleAr = null, string? contentAr = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            TitleAr = titleAr,
            Content = content,
            ContentAr = contentAr,
            CategoryId = categoryId,
            Status = KbArticleStatus.Draft,
            Visibility = visibility,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void SubmitForReview()
    {
        if (Status != KbArticleStatus.Draft)
            throw new InvalidOperationException($"Cannot submit article with status {Status}.");
        if ((Content?.Length ?? 0) < 100)
            throw new InvalidOperationException("Content must be at least 100 characters before submitting.");
        Status = KbArticleStatus.PendingReview;
        RejectionNote = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != KbArticleStatus.PendingReview)
            throw new InvalidOperationException("Only PendingReview articles can be approved.");
        Status = KbArticleStatus.Published;
        PublishedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string rejectionNote)
    {
        if (Status != KbArticleStatus.PendingReview)
            throw new InvalidOperationException("Only PendingReview articles can be rejected.");
        Status = KbArticleStatus.Draft;
        RejectionNote = rejectionNote;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = KbArticleStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }
}
