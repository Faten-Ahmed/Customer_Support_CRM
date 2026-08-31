namespace CRM.Application.KnowledgeBase.DTOs;

public record KbArticleSummaryDto(
    Guid Id,
    string Title,
    string? TitleAr,
    Guid CategoryId,
    string Status,
    string Visibility,
    Guid CreatedByUserId,
    DateTime CreatedAt);
