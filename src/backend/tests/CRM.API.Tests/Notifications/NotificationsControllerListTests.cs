using System.Net;
using CRM.Application.Notifications.DTOs;
using CRM.Application.Notifications.Queries;
using CRM.Application.Notifications;
using CRM.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Notifications;

public class NotificationsControllerListTests
{
    private readonly Mock<IMediator> _mediator = new();

    private HttpClient BuildClient(string role = "Agent")
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
                "Bearer", TestJwtHelper.CreateTestToken(role: role));
        return client;
    }

    [Fact]
    public async Task List_Returns200WithPagedResult()
    {
        _mediator.Setup(m => m.Send(It.IsAny<ListNotificationsQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<NotificationDto>(
                     new List<NotificationDto>(), 0, 1, 20));

        var response = await BuildClient().GetAsync("/api/v1/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_CapsPageSizeAt50()
    {
        _mediator.Setup(m => m.Send(
            It.Is<ListNotificationsQuery>(q => q.PageSize == 50), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new PagedResult<NotificationDto>(
                     new List<NotificationDto>(), 0, 1, 50));

        var response = await BuildClient().GetAsync("/api/v1/notifications?pageSize=200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _mediator.Verify(m => m.Send(
            It.Is<ListNotificationsQuery>(q => q.PageSize == 50), It.IsAny<CancellationToken>()), Times.Once);
    }
}
