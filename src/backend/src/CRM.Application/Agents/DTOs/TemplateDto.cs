namespace CRM.Application.Agents.DTOs;

public record TemplateDto(
    Guid Id,
    string Title,
    string TitleAr,
    string Content,
    string ContentAr,
    string? Category,
    string Scope,
    bool IsActive,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
