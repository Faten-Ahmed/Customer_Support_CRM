using System.Net;
using System.Net.Http.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CRM.API.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private static HttpClient BuildClientThrowing(Exception ex)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("environment", "Testing");
                b.Configure(app =>
                {
                    app.UseMiddleware<CRM.API.Middleware.ExceptionMiddleware>();
                    app.Run(_ => throw ex);
                });
            });
        return factory.CreateClient();
    }

    [Fact]
    public async Task ValidationException_Returns400WithErrors()
    {
        var failures = new[] { new ValidationFailure("Email", "Invalid email") };
        var client = BuildClientThrowing(new ValidationException(failures));

        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.True(body!.ContainsKey("errors"));
    }

    [Fact]
    public async Task UnauthorizedAccessException_Returns401()
    {
        var client = BuildClientThrowing(new UnauthorizedAccessException("Denied"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var client = BuildClientThrowing(new KeyNotFoundException("Not found"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task InvalidOperationException_Returns500()
    {
        var client = BuildClientThrowing(new InvalidOperationException("Bad operation"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_Returns500()
    {
        var client = BuildClientThrowing(new Exception("Boom"));
        var response = await client.GetAsync("/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
