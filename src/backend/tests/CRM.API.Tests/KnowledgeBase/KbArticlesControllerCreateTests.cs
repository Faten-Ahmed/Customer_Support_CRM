using System.Net;
using System.Net.Http.Json;
using CRM.Application.KnowledgeBase.Commands;
using CRM.Application.KnowledgeBase.DTOs;
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

public class KbArticlesControllerCreateTests
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task CreateArticle_ValidRequest_Returns201()
    {
        var articleId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<CreateKbArticleCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new KbArticleSummaryDto(
                     articleId, "How to reset password", null,
                     Guid.NewGuid(), "Draft", "Internal", Guid.NewGuid(), DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync("/api/kb/articles", new
        {
            title = "How to reset password",
            categoryId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateArticle_InvalidCategory_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateKbArticleCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new KeyNotFoundException("Category not found."));

        var response = await BuildClient().PostAsJsonAsync("/api/kb/articles", new
        {
            title = "Title",
            categoryId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
