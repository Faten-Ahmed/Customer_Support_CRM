using System.Net;
using System.Net.Http.Json;
using CRM.Application.KnowledgeBase.DTOs;
using CRM.Application.KnowledgeBase.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbSearchControllerTests
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
    public async Task Search_ValidQuery_Returns200WithResults()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SearchKbQuery>(), default))
                 .ReturnsAsync(new List<KbSearchResultDto>
                 {
                     new(Guid.NewGuid(), "Reset Password", null, Guid.NewGuid(),
                         "Public", DateTime.UtcNow, "To reset your password...")
                 });

        var response = await BuildClient().GetAsync("/api/kb/search?q=reset");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<KbSearchResultDto>>();
        Assert.Single(results!);
    }

    [Fact]
    public async Task Search_QueryTooShort_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<SearchKbQuery>(), default))
                 .ThrowsAsync(new FluentValidation.ValidationException("Query too short."));

        var response = await BuildClient().GetAsync("/api/kb/search?q=a");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
