# Portal Profile Get / Update — Implementation Plan

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

**Story:** US-BE-080  
**Goal:** Implement `GET /api/portal/profile` and `PUT /api/portal/profile` — customers view and update their own profile. `PUT` accepts `fullName`, `phone`, `city` (partial update); email and companyName are ignored. Non-customer role returns 403.

**Architecture:** `GetMyPortalProfileQuery(CustomerId)` and `UpdatePortalProfileCommand(CustomerId, FullName?, Phone?, City?)`. `Customer` entity already exists (from US-BE-010). `PortalController` at `/api/portal` with `[Authorize(Roles = "Customer")]`.

**Tech Stack:** .NET 10, ASP.NET Core, MediatR, EF Core, xUnit, Moq

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.Application/Portal/DTOs/PortalProfileDto.cs` |
| Create | `src/CRM.Application/Portal/Queries/GetMyPortalProfileQuery.cs` |
| Create | `src/CRM.Application/Portal/Commands/UpdatePortalProfileCommand.cs` |
| Create | `src/CRM.API/Controllers/PortalController.cs` |
| Test   | `tests/CRM.Application.Tests/Portal/PortalProfileTests.cs` |
| Test   | `tests/CRM.API.Tests/Portal/PortalControllerTests.cs` |

---

## Task 1: Portal Profile

> Note: `Customer` entity and `ICustomerRepository` are from US-BE-010. Implement that plan first.

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Portal/PortalProfileTests.cs
using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using CRM.Domain.Customers;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Portal;

public class PortalProfileTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly GetMyPortalProfileQueryHandler _getHandler;
    private readonly UpdatePortalProfileCommandHandler _updateHandler;

    public PortalProfileTests()
    {
        _getHandler = new GetMyPortalProfileQueryHandler(_repo.Object);
        _updateHandler = new UpdatePortalProfileCommandHandler(_repo.Object);
    }

    [Fact]
    public async Task Get_ReturnsCustomerProfile()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com");
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _getHandler.Handle(
            new GetMyPortalProfileQuery(customerId), default);

        Assert.Equal("Alice", result.FullName);
        Assert.Equal("alice@example.com", result.Email);
    }

    [Fact]
    public async Task Get_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        var customerId = Guid.NewGuid();
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _getHandler.Handle(new GetMyPortalProfileQuery(customerId), default));
    }

    [Fact]
    public async Task Update_ChangesAllowedFields()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com");
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _updateHandler.Handle(
            new UpdatePortalProfileCommand(customerId, "Alicia", null, "555-9999", "Riyadh"),
            default);

        Assert.Equal("Alicia", result.FullName);
        Assert.Equal("555-9999", result.Phone);
        Assert.Equal("Riyadh", result.City);
        Assert.Equal("alice@example.com", result.Email);       // unchanged
        Assert.Equal("AcmeCorp", result.CompanyName);          // unchanged
        _repo.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Update_NullFields_KeepsExistingValues()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com");
        customer.UpdateCity("Dubai");
        _repo.Setup(r => r.FindByIdAsync(customerId, default)).ReturnsAsync(customer);

        var result = await _updateHandler.Handle(
            new UpdatePortalProfileCommand(customerId, null, null, null, null),
            default);

        Assert.Equal("Alice", result.FullName);
        Assert.Equal("Dubai", result.City);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PortalProfileTests" -v n
```

Expected: FAIL — `GetMyPortalProfileQuery` does not exist yet.

- [ ] **Step 3: Extend Customer entity with profile update methods**

Open `src/CRM.Domain/Customers/Customer.cs`. Add if not present:

```csharp
public string? Phone { get; private set; }
public string? CompanyName { get; private set; }
public string? City { get; private set; }

public void UpdateProfile(string? fullName, string? fullNameAr, string? phone, string? city)
{
    if (fullName is not null) FullName = fullName;
    if (fullNameAr is not null) FullNameAr = fullNameAr;
    if (phone is not null) Phone = phone;
    if (city is not null) City = city;
}

public void UpdateCity(string city) => City = city;
```

- [ ] **Step 4: Create PortalProfileDto**

```csharp
// src/CRM.Application/Portal/DTOs/PortalProfileDto.cs
namespace CRM.Application.Portal.DTOs;

public record PortalProfileDto(
    Guid Id,
    string FullName,
    string? FullNameAr,
    string Email,
    string? Phone,
    string? CompanyName,
    string? CompanyNameAr,
    string? Country,
    string? City);
```

- [ ] **Step 5: Implement GetMyPortalProfileQuery**

```csharp
// src/CRM.Application/Portal/Queries/GetMyPortalProfileQuery.cs
using CRM.Application.Portal.DTOs;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Portal.Queries;

public record GetMyPortalProfileQuery(Guid CustomerId) : IRequest<PortalProfileDto>;

public class GetMyPortalProfileQueryHandler
    : IRequestHandler<GetMyPortalProfileQuery, PortalProfileDto>
{
    private readonly ICustomerRepository _customers;
    public GetMyPortalProfileQueryHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<PortalProfileDto> Handle(
        GetMyPortalProfileQuery query, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(query.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {query.CustomerId} not found.");

        return Map(customer);
    }

    internal static PortalProfileDto Map(Customer c)
        => new(c.Id, c.FullName, c.FullNameAr, c.Email, c.Phone, c.CompanyName, c.CompanyNameAr, c.Country, c.City);
}
```

- [ ] **Step 6: Implement UpdatePortalProfileCommand**

```csharp
// src/CRM.Application/Portal/Commands/UpdatePortalProfileCommand.cs
using CRM.Application.Portal.DTOs;
using CRM.Application.Portal.Queries;
using CRM.Domain.Customers;
using MediatR;

namespace CRM.Application.Portal.Commands;

public record UpdatePortalProfileCommand(
    Guid CustomerId, string? FullName, string? FullNameAr, string? Phone, string? City)
    : IRequest<PortalProfileDto>;

public class UpdatePortalProfileCommandHandler
    : IRequestHandler<UpdatePortalProfileCommand, PortalProfileDto>
{
    private readonly ICustomerRepository _customers;
    public UpdatePortalProfileCommandHandler(ICustomerRepository customers) => _customers = customers;

    public async Task<PortalProfileDto> Handle(
        UpdatePortalProfileCommand cmd, CancellationToken ct)
    {
        var customer = await _customers.FindByIdAsync(cmd.CustomerId, ct)
            ?? throw new KeyNotFoundException($"Customer {cmd.CustomerId} not found.");

        customer.UpdateProfile(cmd.FullName, cmd.FullNameAr, cmd.Phone, cmd.City);
        await _customers.SaveChangesAsync(ct);
        return GetMyPortalProfileQueryHandler.Map(customer);
    }
}
```

- [ ] **Step 6b: Create UpdatePortalProfileCommandValidator**

```csharp
// src/CRM.Application/Portal/Validators/UpdatePortalProfileCommandValidator.cs
using CRM.Application.Portal.Commands;
using FluentValidation;

namespace CRM.Application.Portal.Validators;

public class UpdatePortalProfileCommandValidator : AbstractValidator<UpdatePortalProfileCommand>
{
    public UpdatePortalProfileCommandValidator()
    {
        RuleFor(x => x.FullName).MaximumLength(200).When(x => x.FullName is not null);
        RuleFor(x => x.FullNameAr).MaximumLength(200).When(x => x.FullNameAr is not null);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "PortalProfileTests" -v n
```

Expected: 4 tests PASS.

- [ ] **Step 8: Create PortalController**

```csharp
// src/CRM.API/Controllers/PortalController.cs
using CRM.Application.Portal.Commands;
using CRM.Application.Portal.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CRM.API.Controllers;

[ApiController]
[Route("api/portal")]
[Authorize(Roles = "Customer")]
public class PortalController : ControllerBase
{
    private readonly IMediator _mediator;

    private Guid CurrentCustomerId =>
        Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    public PortalController(IMediator mediator) => _mediator = mediator;

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyPortalProfileQuery(CurrentCustomerId), ct);
        return Ok(new { data = result });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdatePortalProfileCommand(CurrentCustomerId, req.FullName, req.FullNameAr, req.Phone, req.City), ct);
        return Ok(new { data = result });
    }
}

public record UpdateProfileRequest(string? FullName, string? FullNameAr, string? Phone, string? City);
```

- [ ] **Step 9: Write controller test**

```csharp
// tests/CRM.API.Tests/Portal/PortalControllerTests.cs
using System.Net;
using CRM.Application.Portal.DTOs;
using CRM.Application.Portal.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalControllerTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Customer")
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(_mediator.Object);
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task GetProfile_Customer_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetMyPortalProfileQuery>(), default))
                 .ReturnsAsync(new PortalProfileDto(
                     Guid.NewGuid(), "Alice", null, "alice@example.com",
                     "555-0100", "AcmeCorp", null, null, null));

        var response = await BuildClient().GetAsync("/api/portal/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_NonCustomer_Returns403()
    {
        var response = await BuildClient(role: "Agent").GetAsync("/api/portal/profile");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

- [ ] **Step 10: Run controller tests**

```bash
dotnet test tests/CRM.API.Tests/ --filter "PortalControllerTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add src/CRM.Application/Portal/ \
        src/CRM.API/Controllers/PortalController.cs \
        tests/CRM.Application.Tests/Portal/PortalProfileTests.cs \
        tests/CRM.API.Tests/Portal/PortalControllerTests.cs
git commit -m "feat(portal): add GET/PUT /api/portal/profile — customer profile view and partial update"
```
