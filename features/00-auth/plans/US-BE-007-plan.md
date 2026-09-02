# JWT Middleware — Implementation Plan

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

**Story:** US-BE-007  
**Goal:** Configure JWT Bearer authentication middleware in `Program.cs` so that all `[Authorize]` endpoints validate the HS256 JWT, extract claims, and return 401 on missing/invalid tokens and 403 on insufficient roles.

**Architecture:** `AddAuthentication().AddJwtBearer()` configured from `JwtSettings`; `AddAuthorization()` with role-based policies (Admin, Manager, Agent). A `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior runs FluentValidation before every command handler. Global exception middleware maps domain exceptions to HTTP status codes.

**Tech Stack:** .NET 10, ASP.NET Core, Microsoft.AspNetCore.Authentication.JwtBearer, MediatR, FluentValidation, xUnit

---

## File Structure

| Action | Path |
|--------|------|
| Create | `src/CRM.API/Middleware/ExceptionMiddleware.cs` |
| Create | `src/CRM.Application/Common/Behaviors/ValidationBehavior.cs` |
| Modify | `src/CRM.API/Program.cs` |
| Test   | `tests/CRM.API.Tests/Middleware/ExceptionMiddlewareTests.cs` |

---

## Task 1: Validation Pipeline Behavior

**Files:**
- Create: `src/CRM.Application/Common/Behaviors/ValidationBehavior.cs`
- Test: `tests/CRM.Application.Tests/Common/ValidationBehaviorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/CRM.Application.Tests/Common/ValidationBehaviorTests.cs
using CRM.Application.Auth.Commands;
using CRM.Application.Auth.Validators;
using CRM.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;

namespace CRM.Application.Tests.Common;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var validators = new List<IValidator<LoginInternalCommand>>
        {
            new LoginInternalCommandValidator()
        };
        var behavior = new ValidationBehavior<LoginInternalCommand, LoginResponse>(validators);

        var nextCalled = false;
        var response = new LoginResponse("t", "r", false, Guid.NewGuid(), "Ali", "Agent");
        var next = new RequestHandlerDelegate<LoginResponse>(() =>
        {
            nextCalled = true;
            return Task.FromResult(response);
        });

        var result = await behavior.Handle(
            new LoginInternalCommand("agent@crm.test", "P@ssw0rd!"), next, default);

        Assert.True(nextCalled);
        Assert.Equal(response, result);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationException()
    {
        var validators = new List<IValidator<LoginInternalCommand>>
        {
            new LoginInternalCommandValidator()
        };
        var behavior = new ValidationBehavior<LoginInternalCommand, LoginResponse>(validators);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new LoginInternalCommand("not-an-email", "short"),
                () => Task.FromResult(new LoginResponse("", "", false, Guid.NewGuid(), "", "")),
                default));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ValidationBehaviorTests" -v n
```

Expected: FAIL — `ValidationBehavior` does not exist yet.

- [ ] **Step 3: Implement ValidationBehavior**

```csharp
// src/CRM.Application/Common/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;

namespace CRM.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.Application.Tests/ --filter "ValidationBehaviorTests" -v n
```

Expected: 2 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.Application/Common/Behaviors/ValidationBehavior.cs \
        tests/CRM.Application.Tests/Common/ValidationBehaviorTests.cs
git commit -m "feat(application): add MediatR ValidationBehavior pipeline"
```

---

## Task 2: Exception Middleware

**Files:**
- Create: `src/CRM.API/Middleware/ExceptionMiddleware.cs`
- Test: `tests/CRM.API.Tests/Middleware/ExceptionMiddlewareTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// tests/CRM.API.Tests/Middleware/ExceptionMiddlewareTests.cs
using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace CRM.API.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private static HttpClient BuildClientThrowing(Exception ex)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.Configure(app =>
                {
                    app.UseMiddleware<CRM.API.Middleware.ExceptionMiddleware>();
                    app.Run(_ => throw ex);
                });
            });
        return factory.CreateClient();
    }

    [Fact]
    public async Task ValidationException_Returns400WithErrors()
    {
        var failures = new[] { new ValidationFailure("Email", "Invalid email") };
        var client = BuildClientThrowing(new ValidationException(failures));

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.True(body!.ContainsKey("errors"));
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns401()
    {
        var client = BuildClientThrowing(new UnauthorizedAccessException("Denied"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var client = BuildClientThrowing(new KeyNotFoundException("Not found"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidOperationException_Returns400()
    {
        var client = BuildClientThrowing(new InvalidOperationException("Bad operation"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_Returns500()
    {
        var client = BuildClientThrowing(new Exception("Boom"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/CRM.API.Tests/ --filter "ExceptionMiddlewareTests" -v n
```

Expected: FAIL — `ExceptionMiddleware` does not exist yet.

- [ ] **Step 3: Implement ExceptionMiddleware**

```csharp
// src/CRM.API/Middleware/ExceptionMiddleware.cs
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CRM.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponse(context, HttpStatusCode.BadRequest, new
            {
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteResponse(context, HttpStatusCode.Unauthorized, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            await WriteResponse(context, HttpStatusCode.NotFound, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await WriteResponse(context, HttpStatusCode.BadRequest, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponse(context, HttpStatusCode.InternalServerError,
                new { error = "An unexpected error occurred." });
        }
    }

    private static async Task WriteResponse(HttpContext context, HttpStatusCode code, object body)
    {
        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/CRM.API.Tests/ --filter "ExceptionMiddlewareTests" -v n
```

Expected: 5 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CRM.API/Middleware/ExceptionMiddleware.cs \
        tests/CRM.API.Tests/Middleware/ExceptionMiddlewareTests.cs
git commit -m "feat(api): add global ExceptionMiddleware with domain-to-HTTP mapping"
```

---

## Task 3: Program.cs — Wire Up JWT, MediatR, FluentValidation, Middleware

**Files:**
- Modify: `src/CRM.API/Program.cs`

- [ ] **Step 1: Configure Program.cs**

```csharp
// src/CRM.API/Program.cs
using System.Text;
using CRM.Application.Common.Behaviors;
using CRM.API.Middleware;
using CRM.Infrastructure.Identity;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<ITokenService, TokenService>();

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// MediatR — scan Application assembly
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CRM.Application.Auth.Commands.LoginInternalCommand).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(CRM.Application.Auth.Validators.LoginInternalCommandValidator).Assembly);

// MediatR pipeline: validation before every handler
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Infrastructure registrations (repositories, DbContext, etc.) — wired in separate extension methods
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { } // For WebApplicationFactory in tests
```

- [ ] **Step 2: Commit**

```bash
git add src/CRM.API/Program.cs
git commit -m "feat(api): configure JWT auth, MediatR pipeline, and exception middleware in Program.cs"
```
