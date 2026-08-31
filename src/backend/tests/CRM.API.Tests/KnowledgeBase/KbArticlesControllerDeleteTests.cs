using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerDeleteTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.ConfigureServices(services =>
            {
                services.RemoveAll<IMediator>();
                services.AddSingleton(_mediator.Object);
            }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task Delete_DraftArticle_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient().DeleteAsync($"/api/kb/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NotAuthor_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteKbArticleCommand>(), default))
                 .ThrowsAsync(new UnauthorizedAccessException("Not the author."));

        var response = await BuildClient().DeleteAsync($"/api/kb/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_PublishedArticle_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteKbArticleCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Must archive first."));

        var response = await BuildClient().DeleteAsync($"/api/kb/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
