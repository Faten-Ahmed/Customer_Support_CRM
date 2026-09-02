namespace CRM.Domain.Templates;

public enum TemplateScope { Personal, Global }

public class QuickReplyTemplate
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string TitleAr { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string ContentAr { get; private set; } = null!;
    public string? Category { get; private set; }
    public TemplateScope Scope { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private QuickReplyTemplate() { }

    public static QuickReplyTemplate CreateGlobal(
        string title, string titleAr, string content, string contentAr,
        string? category, Guid adminId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            TitleAr = titleAr,
            Content = content,
            ContentAr = contentAr,
            Category = category,
            Scope = TemplateScope.Global,
            CreatedByUserId = adminId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static QuickReplyTemplate CreatePersonal(
        string title, string titleAr, string content, string contentAr,
        string? category, Guid agentId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            TitleAr = titleAr,
            Content = content,
            ContentAr = contentAr,
            Category = category,
            Scope = TemplateScope.Personal,
            CreatedByUserId = agentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string? title, string? titleAr, string? content, string? contentAr, string? category)
    {
        if (title is not null) Title = title;
        if (titleAr is not null) TitleAr = titleAr;
        if (content is not null) Content = content;
        if (contentAr is not null) ContentAr = contentAr;
        if (category is not null) Category = category;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
