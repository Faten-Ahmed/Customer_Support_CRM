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

public class AuthControllerResetPasswordTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
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
        return factory.CreateClient();
    }

    [Fact]
    public async Task ResetPassword_ValidBody_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.FromResult(MediatR.Unit.Value));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token = "valid-token", newPassword = "NewP@ss1!" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ResetPasswordCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Invalid or expired token."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token = "bad", newPassword = "NewP@ss1!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_Returns400()
    {
        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { token = "t", newPassword = "weak" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
