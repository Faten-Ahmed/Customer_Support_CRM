using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Branches.Commands;
using CRM.Application.Admin.Branches.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminBranchesControllerTests
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
    public async Task Create_Branch_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateBranchCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new BranchDto(
                     Guid.NewGuid(), "Riyadh Branch", null, true, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/branches", new { name = "Riyadh Branch" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ToggleBranchCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new BranchActiveResult(Guid.NewGuid(), false));

        var response = await BuildClient()
            .PostAsync($"/api/v1/admin/branches/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
