namespace CRM.Application.Admin.FieldDefinitions.DTOs;

public record FieldDefinitionDto(
    Guid Id,
    Guid DepartmentId,
    Guid? CategoryId,
    string FieldName,
    string? FieldNameAr,
    string FieldType,
    IReadOnlyList<string>? Options,
    bool IsRequired,
    int SortOrder,
    bool IsActive);
