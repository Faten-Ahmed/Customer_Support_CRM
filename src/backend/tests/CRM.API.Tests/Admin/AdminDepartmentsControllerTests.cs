using System.Net;
using System.Net.Http.Json;
using CRM.Application.Admin.Departments.Commands;
using CRM.Application.Admin.Departments.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace CRM.API.Tests.Admin;

public class AdminDepartmentsControllerTests
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
    public async Task Create_UniqueName_Returns201()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateDepartmentCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new DepartmentDto(
                     Guid.NewGuid(), "Technical Support", null, null, null, true, DateTime.UtcNow));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/departments",
            new { name = "Technical Support" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_DuplicateName_Returns409()
    {
        _mediator.Setup(m => m.Send(It.IsAny<CreateDepartmentCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("409: Department exists."));

        var response = await BuildClient().PostAsJsonAsync(
            "/api/v1/admin/departments",
            new { name = "Technical Support" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Deactivate_HasOpenTickets_Returns422()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DeactivateDepartmentCommand>(), It.IsAny<CancellationToken>()))
                 .ThrowsAsync(new InvalidOperationException("Cannot deactivate: 3 open tickets."));

        var response = await BuildClient()
            .PostAsync($"/api/v1/admin/departments/{Guid.NewGuid()}/deactivate", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
