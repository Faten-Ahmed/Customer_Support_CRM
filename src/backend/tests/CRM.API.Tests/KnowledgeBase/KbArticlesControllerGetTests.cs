using System.Net;
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Application.KnowledgeBase.Queries;
using CRM.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerGetTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient()
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
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task GetById_Returns200WithArticle()
    {
        var articleId = Guid.NewGuid();
        _mediator.Setup(m => m.Send(It.IsAny<GetKbArticleQuery>(), default))
                 .ReturnsAsync(new KbArticleDetailDto(
                     articleId, "Title", null, "Content...", null,
                     Guid.NewGuid(), "Published", "Internal",
                     Guid.NewGuid(), DateTime.UtcNow, null,
                     DateTime.UtcNow.AddDays(-1), DateTime.UtcNow));

        var response = await BuildClient().GetAsync($"/api/kb/articles/{articleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_Returns200WithPagedResult()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListKbArticlesQuery>(), default))
                 .ReturnsAsync(new PagedResult<KbArticleSummaryDto>(
                     new List<KbArticleSummaryDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/kb/articles?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
