namespace CRM.Application.KnowledgeBase.DTOs;

public record KbArticleDetailDto(
    Guid Id,
    string Title,
    string? TitleAr,
    string? Content,
    string? ContentAr,
    Guid CategoryId,
    string Status,
    string Visibility,
    Guid CreatedByUserId,
    DateTime? PublishedAt,
    string? RejectionNote,
    DateTime CreatedAt,
    DateTime UpdatedAt);
