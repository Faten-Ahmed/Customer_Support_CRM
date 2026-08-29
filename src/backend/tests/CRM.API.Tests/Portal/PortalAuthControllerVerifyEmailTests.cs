using System.Net;
using System.Net.Http.Json;
using CRM.Application.Customers.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Portal;

public class PortalAuthControllerVerifyEmailTests
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
    public async Task VerifyEmail_ValidToken_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<VerifyEmailCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/portal/verify-email", new
        {
            token = "valid-verify-token"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_TokenNotFound_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<VerifyEmailCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Token not found."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/portal/verify-email", new
        {
            token = "not-found-token"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredOrUsedToken_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<VerifyEmailCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Token is expired or already used."));

        var client = BuildClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/portal/verify-email", new
        {
            token = "expired-token"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
