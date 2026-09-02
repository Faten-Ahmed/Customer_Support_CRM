using System.Net;
using System.Net.Http.Json;
using CRM.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerChangeFirstLoginPasswordTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(bool authenticated = true)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("environment", "Testing");
                b.ConfigureServices(services =>
                {
                    services.RemoveAll<IMediator>();
                    services.AddSingleton<IMediator>(_mediator.Object);

                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions>();
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<CRM.Infrastructure.Persistence.AppDbContext>>();
                    services.RemoveAll<CRM.Infrastructure.Persistence.AppDbContext>();

                    services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
                    services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                });
            });

        var client = factory.CreateClient();
        if (authenticated)
        {
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", TestJwtHelper.CreateTestToken());
        }
        return client;
    }

    [Fact]
    public async Task ChangeFirstLoginPassword_ValidBody_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ChangeFirstLoginPasswordCommand>(),
                                    It.IsAny<CancellationToken>()))
                 .Returns(Task.FromResult(MediatR.Unit.Value));

        var client = BuildClient(authenticated: false);
        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password-first-login",
            new { email = "agent@crm.test", currentPassword = "OldP@ss1!", newPassword = "NewP@ss2!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangeFirstLoginPassword_WrongPassword_Returns401()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ChangeFirstLoginPasswordCommand>(),
                                    It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new UnauthorizedAccessException("Wrong password"));

        var client = BuildClient(authenticated: false);
        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password-first-login",
            new { email = "agent@crm.test", currentPassword = "wrong", newPassword = "NewP@ss2!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeFirstLoginPassword_NoToken_IsAllowedAnonymously()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ChangeFirstLoginPasswordCommand>(),
                                    It.IsAny<CancellationToken>()))
                 .Returns(Task.FromResult(MediatR.Unit.Value));

        var client = BuildClient(authenticated: false);
        var response = await client.PostAsJsonAsync("/api/v1/auth/change-password-first-login",
            new { email = "agent@crm.test", currentPassword = "OldP@ss1!", newPassword = "NewP@ss2!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
