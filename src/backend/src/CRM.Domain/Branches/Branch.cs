namespace CRM.Domain.Branches;

public class Branch
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private Branch() { }

    public static Branch Create(string name, string? nameAr) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        NameAr = nameAr,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string? name, string? nameAr)
    {
        if (name is not null) Name = name;
        if (nameAr is not null) NameAr = nameAr;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
}
