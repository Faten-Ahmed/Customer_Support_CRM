using System.Net;
using CRM.Application.Admin.Channels.DTOs;
using CRM.Application.Admin.Channels.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminChannelsControllerTests
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
    public async Task Status_Returns200WithFiveChannels()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetChannelStatusQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new ChannelStatusListDto(new List<ChannelStatusDto>
                 {
                     new("email", true, true, null, null, null, null),
                     new("whatsapp", true, true, null, null, null, null),
                     new("sms", true, true, null, null, null, null),
                     new("liveChat", true, true, null, 2, 0, null),
                     new("portal", true, true, null, null, null, null),
                 }));

        var response = await BuildClient().GetAsync("/api/v1/admin/channels/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Status_NonAdmin_Returns403()
    {
        var response = await BuildClient(role: "Agent")
            .GetAsync("/api/v1/admin/channels/status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
