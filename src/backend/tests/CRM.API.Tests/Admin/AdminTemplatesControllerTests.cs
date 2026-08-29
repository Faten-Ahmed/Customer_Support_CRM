using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Templates.Commands;
using CRM.Application.Admin.Templates.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminTemplatesControllerTests
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
    public async Task Create_GlobalTemplate_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateGlobalTemplateCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new TemplateDto(
                     Guid.NewGuid(), "Standard Greeting", "Hello!", "Greeting",
                     "Global", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/templates",
            new { title = "Standard Greeting", content = "Hello!", category = "Greeting" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Delete_PersonalTemplate_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeleteGlobalTemplateCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException(
                     "Only Global templates can be deleted via this admin endpoint."));

        var response = await BuildClient()
            .DeleteAsync($"/api/v1/admin/templates/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
