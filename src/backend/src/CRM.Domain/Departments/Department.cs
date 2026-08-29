namespace CRM.Domain.Departments;

public class Department
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public string? Description { get; private set; }
    public Guid? BusinessHoursId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private Department() { }

    public static Department Create(
        string name, string? nameAr, string? description, Guid? businessHoursId)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameAr = nameAr,
            Description = description,
            BusinessHoursId = businessHoursId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(string? name, string? nameAr, string? description, Guid? businessHoursId)
    {
        if (name is not null) Name = name;
        if (nameAr is not null) NameAr = nameAr;
        if (description is not null) Description = description;
        if (businessHoursId.HasValue) BusinessHoursId = businessHoursId;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
