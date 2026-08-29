using System.Net;
using CRM.Application.Admin.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerDeactivateTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Admin")
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
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task Deactivate_Agent_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateUserCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new UserActiveResult(Guid.NewGuid(), false));

        var response = await BuildClient()
            .PostAsync($"/api/v1/admin/users/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Self_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateUserCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException(
                     "CANNOT_DEACTIVATE_SELF: Cannot deactivate own account."));

        var response = await BuildClient()
            .PostAsync($"/api/v1/admin/users/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Reactivate_DeactivatedUser_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ReactivateUserCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new UserActiveResult(Guid.NewGuid(), true));

        var response = await BuildClient()
            .PostAsync($"/api/v1/admin/users/{Guid.NewGuid()}/reactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
