using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerArchiveTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Manager")
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("environment", "Testing");
                b.ConfigureServices(services =>
                {
                    services.RemoveAll<IMediator>();
                    services.AddSingleton<IMediator>(_mediator.Object);
                    services.RemoveAll<DbContextOptions>();
                    services.RemoveAll<DbContextOptions<CRM.Infrastructure.Persistence.AppDbContext>>();
                    services.RemoveAll<CRM.Infrastructure.Persistence.AppDbContext>();
                    services.RemoveAll<IConnectionMultiplexer>();
                    services.RemoveAll<IDistributedCache>();
                });
            });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task Archive_ManagerRole_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ArchiveKbArticleCommand>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient()
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Archive_AgentRole_Returns403()
    {
        var response = await BuildClient("Agent")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/archive", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
