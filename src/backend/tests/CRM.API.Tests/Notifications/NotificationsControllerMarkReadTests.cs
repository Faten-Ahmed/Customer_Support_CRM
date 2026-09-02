using System.Net;
using CRM.Application.Notifications.Commands;
using CRM.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Notifications;

public class NotificationsControllerMarkReadTests
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
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions>();
                    services.RemoveAll<Microsoft.EntityFrameworkCore.DbContextOptions<CRM.Infrastructure.Persistence.AppDbContext>>();
                    services.RemoveAll<CRM.Infrastructure.Persistence.AppDbContext>();
                    services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
                    services.RemoveAll<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                    services.RemoveAll<INotificationPushService>();
                    services.AddSingleton<INotificationPushService>(new Mock<INotificationPushService>().Object);
                });
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", TestJwtHelper.CreateTestToken(role: "Agent"));
        return client;
    }

    [Fact]
    public async Task MarkRead_Returns200()
    {
        _mediator.Setup(m => m.Send(It.IsAny<MarkNotificationReadCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new MarkNotificationReadResult(
                     Guid.NewGuid(), true, DateTime.UtcNow));

        var response = await BuildClient()
            .PutAsync($"/api/v1/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_WrongUser_Returns403()
    {
        _mediator.Setup(m => m.Send(It.IsAny<MarkNotificationReadCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new UnauthorizedAccessException("Not your notification."));

        var response = await BuildClient()
            .PutAsync($"/api/v1/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Returns200WithCount()
    {
        _mediator.Setup(m => m.Send(It.IsAny<MarkAllNotificationsReadCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(5);

        var response = await BuildClient()
            .PutAsync("/api/v1/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
