namespace CRM.Domain.Tickets;

public enum FieldType { Text, Number, Date, Dropdown, Checkbox }

public class TicketFieldDefinition
{
    public Guid Id { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string FieldName { get; private set; } = null!;
    public string? FieldNameAr { get; private set; }
    public FieldType FieldType { get; private set; }
    public IReadOnlyList<string>? Options { get; private set; }
    public bool IsRequired { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private TicketFieldDefinition() { }

    public static TicketFieldDefinition Create(
        Guid departmentId, Guid? categoryId, string fieldName, string? fieldNameAr,
        FieldType fieldType, IReadOnlyList<string>? options, bool isRequired, int sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            CategoryId = categoryId,
            FieldName = fieldName,
            FieldNameAr = fieldNameAr,
            FieldType = fieldType,
            Options = options,
            IsRequired = isRequired,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(string? fieldName, string? fieldNameAr,
        IReadOnlyList<string>? options, bool? isRequired, int? sortOrder)
    {
        if (fieldName is not null) FieldName = fieldName;
        if (fieldNameAr is not null) FieldNameAr = fieldNameAr;
        if (options is not null) Options = options;
        if (isRequired.HasValue) IsRequired = isRequired.Value;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }

    public void Deactivate() => IsActive = false;
}
