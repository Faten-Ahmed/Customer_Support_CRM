namespace CRM.Application.KnowledgeBase.DTOs;

public record KbSearchResultDto(
    Guid Id,
    string Title,
    string? TitleAr,
    Guid CategoryId,
    string Visibility,
    DateTime? PublishedAt,
    string Excerpt);
