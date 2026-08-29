namespace CRM.Application.Admin.Categories.DTOs;

public record CategoryDto(
    Guid Id, string Name, string? NameAr,
    Guid? ParentCategoryId, int SortOrder, bool IsActive,
    IReadOnlyList<CategoryDto>? Children = null);
