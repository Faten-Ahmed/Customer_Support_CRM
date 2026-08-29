namespace CRM.Application.Admin.Templates.DTOs;

public record TemplateDto(
    Guid Id, string Title, string Content, string? Category,
    string Scope, Guid CreatedByUserId, DateTime CreatedAt, DateTime UpdatedAt);
