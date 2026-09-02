namespace CRM.Domain.Categories;

public class TicketCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private TicketCategory() { }

    public static TicketCategory Create(
        string name, string? nameAr, Guid? parentCategoryId, int sortOrder)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameAr = nameAr,
            ParentCategoryId = parentCategoryId,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(string? name, string? nameAr, int? sortOrder)
    {
        if (name is not null) Name = name;
        if (nameAr is not null) NameAr = nameAr;
        if (sortOrder.HasValue) SortOrder = sortOrder.Value;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
