using System.Net;
using System.Net.Http.Json;
using CRM.Application.KnowledgeBase.Commands;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CRM.API.Tests.KnowledgeBase;

public class KbArticlesControllerRejectTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Manager")
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
    public async Task Reject_ValidNote_Returns204()
    {
        _mediator.Setup(m => m.Send(It.IsAny<RejectKbArticleCommand>(), default))
                 .Returns(Task.CompletedTask);

        var response = await BuildClient().PostAsJsonAsync(
            $"/api/kb/articles/{Guid.NewGuid()}/reject",
            new { rejectionNote = "Please add more context and examples here." });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Reject_AgentRole_Returns403()
    {
        var response = await BuildClient("Agent").PostAsJsonAsync(
            $"/api/kb/articles/{Guid.NewGuid()}/reject",
            new { rejectionNote = "Too short" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
