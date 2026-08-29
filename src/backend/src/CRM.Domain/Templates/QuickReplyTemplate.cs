namespace CRM.Domain.Templates;

public enum TemplateScope { Personal, Global }

public class QuickReplyTemplate
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? Category { get; private set; }
    public TemplateScope Scope { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private QuickReplyTemplate() { }

    public static QuickReplyTemplate CreateGlobal(
        string title, string content, string? category, Guid adminId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Category = category,
            Scope = TemplateScope.Global,
            CreatedByUserId = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public static QuickReplyTemplate CreatePersonal(
        string title, string content, string? category, Guid agentId)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Category = category,
            Scope = TemplateScope.Personal,
            CreatedByUserId = agentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    public void Update(string? title, string? content, string? category)
    {
        if (title is not null) Title = title;
        if (content is not null) Content = content;
        if (category is not null) Category = category;
        UpdatedAt = DateTime.UtcNow;
    }
}
