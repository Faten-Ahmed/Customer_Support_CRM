# Custom Field Validation — Implementation Plan

## Internationalization (i18n) Requirements

- ✅ All UI text must use `$localize` or Angular i18n
- ✅ Arabic text must support RTL layout (Angular Material handles this with `dir="rtl"`)
- ✅ English text must support LTR layout
- ✅ No hardcoded text in templates or components
- ✅ All labels, buttons, messages, and notifications must be translatable

## Angular Material Requirements

- ✅ All UI components must use Angular Material (MatButton, MatCard, MatFormField, MatInput, MatTable, etc.)
- ✅ NO Tailwind CSS
- ✅ NO custom CSS frameworks
- ✅ Use Angular Material theming for branding (colors, typography)

---


> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Story:** US-BE-038  
**Goal:** Implement `CustomFieldValidator` — a domain service that validates `Ticket.CustomFieldValues` against active `TicketFieldDefinition` records, collecting all errors and throwing a `ValidationException` with the full list.

**Architecture:** `CustomFieldValidator.ValidateAsync(values, departmentId, categoryId?, ct)` loads definitions from `ITicketFieldDefinitionRepository`, checks required fields, validates type constraints (Dropdown options, Number parsability, Date ISO 8601, Checkbox true/false), collects all violations, and throws a `FluentValidation.ValidationException` with the full error list. Called from `CreateTicketCommandHandler` and `UpdateTicketCommandHandler`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, FluentValidation, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Domain/Tickets/TicketFieldDefinition.cs` |
| Create | `src/CRM.Domain/Tickets/ITicketFieldDefinitionRepository.cs` |
| Create | `src/CRM.Application/Tickets/Services/CustomFieldValidator.cs` |
| Modify | `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs` |
| Modify | `src/CRM.Application/Tickets/Commands/UpdateTicketCommand.cs` |
| Test   | `tests/CRM.Application.Tests/Tickets/CustomFieldValidatorTests.cs` |

---

## Task 1: TicketFieldDefinition Entity + CustomFieldValidator

**Files:**
- Create: `src/CRM.Domain/Tickets/TicketFieldDefinition.cs`
- Create: `src/CRM.Domain/Tickets/ITicketFieldDefinitionRepository.cs`
- Create: `src/CRM.Application/Tickets/Services/CustomFieldValidator.cs`
- Test: `tests/CRM.Application.Tests/Tickets/CustomFieldValidatorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Tickets/CustomFieldValidatorTests.cs
using CRM.Application.Tickets.Services;
using CRM.Domain.Tickets;
using FluentValidation;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Tickets;

public class CustomFieldValidatorTests
{
    private readonly Mock<ITicketFieldDefinitionRepository> _repo = new();
    private readonly CustomFieldValidator _validator;

    public CustomFieldValidatorTests()
    {
        _validator = new CustomFieldValidator(_repo.Object);
    }

    private void SetupDefinitions(params TicketFieldDefinition[] defs)
    {
        _repo.Setup(r => r.GetActiveAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), default))
             .ReturnsAsync(defs.ToList());
    }

    [Fact]
    public async Task Validate_RequiredFieldMissing_ThrowsValidationException()
    {
        var def = TicketFieldDefinition.Create("Account Number", FieldType.Text, isRequired: true);
        SetupDefinitions(def);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(new Dictionary<string, string>(), null, null, default));

        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("Account Number"));
    }

    [Fact]
    public async Task Validate_DropdownInvalidOption_ThrowsValidationException()
    {
        var def = TicketFieldDefinition.Create("Region", FieldType.Dropdown, isRequired: false,
            options: new[] { "North", "South" });
        SetupDefinitions(def);

        var values = new Dictionary<string, string> { [def.Id.ToString()] = "East" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(values, null, null, default));

        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("Region"));
    }

    [Fact]
    public async Task Validate_NumberFieldNonNumeric_ThrowsValidationException()
    {
        var def = TicketFieldDefinition.Create("Order ID", FieldType.Number, isRequired: false);
        SetupDefinitions(def);

        var values = new Dictionary<string, string> { [def.Id.ToString()] = "abc" };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(values, null, null, default));
    }

    [Fact]
    public async Task Validate_DateFieldInvalidDate_ThrowsValidationException()
    {
        var def = TicketFieldDefinition.Create("Due Date", FieldType.Date, isRequired: false);
        SetupDefinitions(def);

        var values = new Dictionary<string, string> { [def.Id.ToString()] = "not-a-date" };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(values, null, null, default));
    }

    [Fact]
    public async Task Validate_CheckboxInvalidValue_ThrowsValidationException()
    {
        var def = TicketFieldDefinition.Create("Urgent", FieldType.Checkbox, isRequired: false);
        SetupDefinitions(def);

        var values = new Dictionary<string, string> { [def.Id.ToString()] = "yes" };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(values, null, null, default));
    }

    [Fact]
    public async Task Validate_UnknownFieldId_ThrowsValidationException()
    {
        SetupDefinitions(); // no definitions

        var values = new Dictionary<string, string> { [Guid.NewGuid().ToString()] = "value" };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(values, null, null, default));

        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("Unknown field"));
    }

    [Fact]
    public async Task Validate_AllValidValues_DoesNotThrow()
    {
        var numDef = TicketFieldDefinition.Create("Order ID", FieldType.Number, isRequired: true);
        var dropDef = TicketFieldDefinition.Create("Region", FieldType.Dropdown, isRequired: false,
            options: new[] { "North", "South" });
        SetupDefinitions(numDef, dropDef);

        var values = new Dictionary<string, string>
        {
            [numDef.Id.ToString()] = "12345",
            [dropDef.Id.ToString()] = "North"
        };

        // Should not throw
        await _validator.ValidateAsync(values, null, null, default);
    }

    [Fact]
    public async Task Validate_MultipleErrors_AllReturnedTogether()
    {
        var def1 = TicketFieldDefinition.Create("Field A", FieldType.Number, isRequired: true);
        var def2 = TicketFieldDefinition.Create("Field B", FieldType.Checkbox, isRequired: false);
        SetupDefinitions(def1, def2);

        var values = new Dictionary<string, string>
        {
            [def2.Id.ToString()] = "maybe" // invalid checkbox; Field A missing (required)
        };

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            _validator.ValidateAsync(values, null, null, default));

        Assert.True(ex.Errors.Count() >= 2);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CustomFieldValidatorTests" -v n
```

Expected: FAIL — `TicketFieldDefinition` and `CustomFieldValidator` do not exist yet.

- [ ] **Step 3: Create TicketFieldDefinition entity**

```csharp
// src/CRM.Domain/Tickets/TicketFieldDefinition.cs
namespace CRM.Domain.Tickets;

public enum FieldType { Text, Number, Date, Dropdown, Checkbox }

public class TicketFieldDefinition
{
    public Guid Id { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public FieldType FieldType { get; private set; }
    public bool IsRequired { get; private set; }
    public string[]? Options { get; private set; }
    public bool IsActive { get; private set; }

    private TicketFieldDefinition() { }

    public static TicketFieldDefinition Create(
        string name,
        FieldType fieldType,
        bool isRequired,
        Guid? departmentId = null,
        Guid? categoryId = null,
        string[]? options = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            FieldType = fieldType,
            IsRequired = isRequired,
            DepartmentId = departmentId,
            CategoryId = categoryId,
            Options = options,
            IsActive = true
        };

    public void Update(string name, bool isRequired, string[]? options)
    {
        Name = name;
        IsRequired = isRequired;
        Options = options;
    }

    public void Deactivate() => IsActive = false;
}
```

- [ ] **Step 4: Create ITicketFieldDefinitionRepository**

```csharp
// src/CRM.Domain/Tickets/ITicketFieldDefinitionRepository.cs
namespace CRM.Domain.Tickets;

public interface ITicketFieldDefinitionRepository
{
    Task<IReadOnlyList<TicketFieldDefinition>> GetActiveAsync(
        Guid? departmentId, Guid? categoryId, CancellationToken ct = default);

    Task<TicketFieldDefinition?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TicketFieldDefinition definition, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 5: Implement CustomFieldValidator**

```csharp
// src/CRM.Application/Tickets/Services/CustomFieldValidator.cs
using CRM.Domain.Tickets;
using FluentValidation;
using FluentValidation.Results;

namespace CRM.Application.Tickets.Services;

public class CustomFieldValidator
{
    private readonly ITicketFieldDefinitionRepository _definitions;

    public CustomFieldValidator(ITicketFieldDefinitionRepository definitions)
        => _definitions = definitions;

    public async Task ValidateAsync(
        Dictionary<string, string> customFieldValues,
        Guid? departmentId,
        Guid? categoryId,
        CancellationToken ct)
    {
        var definitions = await _definitions.GetActiveAsync(departmentId, categoryId, ct);
        var errors = new List<ValidationFailure>();
        var defById = definitions.ToDictionary(d => d.Id.ToString());

        foreach (var key in customFieldValues.Keys)
        {
            if (!defById.ContainsKey(key))
                errors.Add(new ValidationFailure("CustomField",
                    $"Unknown field ID: {key}"));
        }

        foreach (var def in definitions)
        {
            customFieldValues.TryGetValue(def.Id.ToString(), out var value);

            if (def.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ValidationFailure("CustomField",
                    $"Required field '{def.Name}' is missing.",
                    "REQUIRED_FIELD_MISSING"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(value)) continue;

            switch (def.FieldType)
            {
                case FieldType.Dropdown when def.Options != null && !def.Options.Contains(value):
                    errors.Add(new ValidationFailure("CustomField",
                        $"Field '{def.Name}': '{value}' is not a valid option."));
                    break;
                case FieldType.Number when !decimal.TryParse(value, out _):
                    errors.Add(new ValidationFailure("CustomField",
                        $"Field '{def.Name}': '{value}' is not a valid number."));
                    break;
                case FieldType.Date when !DateTime.TryParse(
                    value, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out _):
                    errors.Add(new ValidationFailure("CustomField",
                        $"Field '{def.Name}': '{value}' is not a valid ISO 8601 date."));
                    break;
                case FieldType.Checkbox when value != "true" && value != "false":
                    errors.Add(new ValidationFailure("CustomField",
                        $"Field '{def.Name}': value must be 'true' or 'false'."));
                    break;
            }
        }

        if (errors.Any())
            throw new ValidationException(errors);
    }
}
```

- [ ] **Step 6: Call validator from ticket command handlers**

In `src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs`:

```csharp
// Add to handler constructor: CustomFieldValidator _customFieldValidator
// In Handle(), before creating the ticket:
if (cmd.CustomFieldValues?.Any() == true || definitions require fields)
    await _customFieldValidator.ValidateAsync(
        cmd.CustomFieldValues ?? new Dictionary<string, string>(),
        cmd.DepartmentId, cmd.CategoryId, ct);
```

Same pattern in `UpdateTicketCommand` handler.

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "CustomFieldValidatorTests" -v n
```

Expected: 7 tests PASS.

- [ ] **Step 8: Commit**

```bash
git add src/CRM.Domain/Tickets/TicketFieldDefinition.cs \
        src/CRM.Domain/Tickets/ITicketFieldDefinitionRepository.cs \
        src/CRM.Application/Tickets/Services/CustomFieldValidator.cs \
        src/CRM.Application/Tickets/Commands/CreateTicketInternalCommand.cs \
        src/CRM.Application/Tickets/Commands/UpdateTicketCommand.cs \
        tests/CRM.Application.Tests/Tickets/CustomFieldValidatorTests.cs
git commit -m "feat(tickets): add CustomFieldValidator with per-type validation and full error collection"
```
