namespace CRM.Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string FirstNameAr { get; private set; } = null!;
    public string LastNameAr { get; private set; } = null!;
    public string? JobTitle { get; private set; }
    public string? JobTitleAr { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool RequiresPasswordChange { get; private set; }
    public AvailabilityStatus AvailabilityStatus { get; private set; }
    public DateTime? LastAvailabilityChange { get; private set; }
    public DateTime? LastAssignedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<UserDepartment> _departments = new();
    public IReadOnlyList<UserDepartment> Departments => _departments.AsReadOnly();

    private readonly List<UserSkill> _skills = new();
    public IReadOnlyList<UserSkill> Skills => _skills.AsReadOnly();

    private User() { }

    public void SetAvailability(AvailabilityStatus status)
    {
        AvailabilityStatus = status;
        LastAvailabilityChange = DateTime.UtcNow;
    }

    public void RecordAssignment()
    {
        LastAssignedAt = DateTime.UtcNow;
    }

    public void SetPassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        RequiresPasswordChange = false;
    }

    public void SetPassword(string passwordHash, bool mustChange)
    {
        PasswordHash = passwordHash;
        RequiresPasswordChange = mustChange;
    }

    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;

    public void UpdateProfile(
        string firstName, string lastName,
        string? firstNameAr = null, string? lastNameAr = null,
        string? jobTitle = null, string? jobTitleAr = null)
    {
        FirstName = firstName;
        LastName = lastName;
        if (firstNameAr is not null) FirstNameAr = firstNameAr;
        if (lastNameAr is not null) LastNameAr = lastNameAr;
        JobTitle = jobTitle;
        JobTitleAr = jobTitleAr;
    }

    public void ReplaceDepartments(IEnumerable<UserDepartment> newDepartments)
    {
        _departments.Clear();
        _departments.AddRange(newDepartments);
    }

    public void ReplaceSkills(IEnumerable<UserSkill> newSkills)
    {
        _skills.Clear();
        _skills.AddRange(newSkills);
    }

    public static User CreateInternal(
        Guid id, string firstName, string lastName, string email, UserRole role,
        string? firstNameAr = null, string? lastNameAr = null,
        string? jobTitle = null, string? jobTitleAr = null)
        => new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            FirstNameAr = firstNameAr ?? string.Empty,
            LastNameAr = lastNameAr ?? string.Empty,
            JobTitle = jobTitle,
            JobTitleAr = jobTitleAr,
            Email = email,
            Role = role,
            IsActive = true,
            AvailabilityStatus = AvailabilityStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

    public static User CreateSeeded(
        string email,
        string passwordHash,
        string firstName,
        string firstNameAr,
        string lastName,
        string lastNameAr,
        UserRole role,
        bool requiresPasswordChange)
        => new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            FirstNameAr = firstNameAr,
            LastName = lastName,
            LastNameAr = lastNameAr,
            Role = role,
            IsActive = true,
            RequiresPasswordChange = requiresPasswordChange,
            AvailabilityStatus = AvailabilityStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

    // Factory used only in tests — allows setting arbitrary state
    public static User CreateForTest(
        string email,
        string passwordHash,
        UserRole role,
        bool isActive = true,
        bool requiresPasswordChange = false,
        Guid? id = null,
        string firstName = "Test",
        string firstNameAr = "اختبار",
        string lastName = "User",
        string lastNameAr = "مستخدم")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            FirstNameAr = firstNameAr,
            LastName = lastName,
            LastNameAr = lastNameAr,
            Role = role,
            IsActive = isActive,
            RequiresPasswordChange = requiresPasswordChange,
            AvailabilityStatus = AvailabilityStatus.Available,
            CreatedAt = DateTime.UtcNow
        };
}
