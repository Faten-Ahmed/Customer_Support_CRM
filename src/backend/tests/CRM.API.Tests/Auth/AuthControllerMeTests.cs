using System.Net;
using System.Net.Http.Json;
using CRM.Application.Auth.DTOs;
using CRM.Application.Auth.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Auth;

public class AuthControllerMeTests
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
    public async Task GetMe_Authenticated_Returns200WithProfile()
    {
        var userId = TestJwtHelper.TestUserId;
        _mediator.Setup(m => m.Send(It.Is<GetCurrentUserQuery>(q => q.UserId == userId),
                                    It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new CurrentUserDto(userId, "a@b.com", "Ali", "علي", "Hassan", "حسن",
                     null, null, "Agent", true, false, null, "Support"));

        var client = BuildClient();
        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.Equal("a@b.com", body!.Email);
    }

    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var client = BuildClient(authenticated: false);
        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
