namespace CRM.Application.Admin.Templates.DTOs;

public record TemplateDto(
    Guid Id,
    string Title, string TitleAr,
    string Content, string ContentAr,
    string? Category,
    string Scope, Guid CreatedByUserId,
    bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt);
