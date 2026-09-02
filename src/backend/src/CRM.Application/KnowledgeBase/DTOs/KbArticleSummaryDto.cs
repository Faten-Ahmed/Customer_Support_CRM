namespace CRM.Application.KnowledgeBase.DTOs;

public record KbArticleSummaryDto(
    Guid Id,
    string Title,
    string? TitleAr,
    Guid CategoryId,
    string? CategoryName,
    string Status,
    string Visibility,
    Guid CreatedByUserId,
    string? AuthorName,
    DateTime? PublishedAt,
    DateTime CreatedAt);
