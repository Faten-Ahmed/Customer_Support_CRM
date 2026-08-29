using System.Net;
using CRM.Application.Admin.Users.Queries;
using CRM.Application.Admin.Users.DTOs;
using CRM.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminUsersControllerGetTests
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
    public async Task List_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListUsersQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<UserSummaryDto>(
                     new List<UserSummaryDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetUserQuery>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("User not found."));

        var response = await BuildClient()
            .GetAsync($"/api/v1/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
