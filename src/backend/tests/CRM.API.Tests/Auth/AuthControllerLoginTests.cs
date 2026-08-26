using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Application.Auth.Commands;
using CRM.Application.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerLoginTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                // Use "Testing" environment so appsettings.Testing.json is loaded first —
                // it sets Redis to abortConnect=false, preventing eager connection failure.
                b.UseSetting("environment", "Testing");

                b.ConfigureServices(services =>
                {
                    // Replace IMediator with mock — runs after all app DI is registered
                    services.RemoveAll<IMediator>();
                    services.AddSingleton<IMediator>(_mediator.Object);

                    // Remove EF Core DbContext to avoid needing SQL Server
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions>();
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<CRM.Infrastructure.Persistence.AppDbContext>>();
                    services.RemoveAll<CRM.Infrastructure.Persistence.AppDbContext>();

                    // Remove distributed cache / Redis wrappers
                    services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
                    services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                });
            });
        return factory.CreateClient();
    }

    // Helper to set up mediator for any LoginInternalCommand request
    private void SetupLoginSuccess()
    {
        _mediator
            .Setup(m => m.Send(
                It.IsAny<LoginInternalCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginResponse(
                "jwt", "raw-refresh", false,
                Guid.NewGuid(), "Ali Hassan", "Agent"));
    }

    [Fact]
    public async Task LoginInternal_ValidBody_Returns200WithAccessToken()
    {
        SetupLoginSuccess();

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/auth/login-internal",
            new { email = "agent@crm.test", password = "P@ssw0rd!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.Equal("jwt", body!["accessToken"].GetString());
    }

    [Fact]
    public async Task LoginInternal_WrongPassword_Returns401()
    {
        _mediator
            .Setup(m => m.Send(
                It.IsAny<LoginInternalCommand>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/auth/login-internal",
            new { email = "agent@crm.test", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LoginInternal_ValidBody_SetsRefreshTokenCookie()
    {
        SetupLoginSuccess();

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/auth/login-internal",
            new { email = "agent@crm.test", password = "P@ssw0rd!" });

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("refreshToken="));
    }
}
