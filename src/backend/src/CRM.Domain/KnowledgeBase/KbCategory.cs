namespace CRM.Domain.KnowledgeBase;

public class KbCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private KbCategory() { }

    public static KbCategory Create(string name)
        => new() { Id = Guid.NewGuid(), Name = name, IsActive = true };
}
