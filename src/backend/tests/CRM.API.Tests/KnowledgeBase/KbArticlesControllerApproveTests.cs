using System.Net;
using CRM.Application.KnowledgeBase.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerApproveTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role)
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
    public async Task Approve_ManagerRole_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ApproveKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient("Manager")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Approve_AgentRole_Returns403()
    {
        var response = await BuildClient("Agent")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Approve_InvalidStatus_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ApproveKbArticleCommand>(), default))
                 .ThrowsAsync(new InvalidOperationException("Not in PendingReview status."));

        var response = await BuildClient("Manager")
            .PostAsync($"/api/kb/articles/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
