using CRM.API.Hubs;
using CRM.API.Middleware;
using CRM.Application;
using CRM.Application.Sla.Jobs;
using CRM.Infrastructure;
using CRM.Infrastructure.Persistence.Seed;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext());

    // Application + Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // JWT Authentication
    var jwtSection = builder.Configuration.GetSection("Jwt");
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
                ClockSkew = TimeSpan.Zero,
            };
            // Allow SignalR connections to pass JWT via query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) &&
                        ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        ctx.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
            opts.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter()));
    builder.Services.AddFluentValidationAutoValidation();

    // SignalR (hubs mapped after feature implementation)
    builder.Services.AddSignalR();

    // Swagger — dev only (Bearer security definition added once Swashbuckle OpenAPI 2.x API is confirmed)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // CORS — Angular dev server + production origins
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200"];

    builder.Services.AddCors(options =>
        options.AddPolicy("AllowAngular", policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AZM CRM API v1"));
    }

    app.UseMiddleware<ExceptionMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowAngular");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapHub<NotificationHub>("/hubs/notifications");
    // app.MapHub<ChatHub>("/hubs/chat");
    // app.MapHub<DashboardHub>("/hubs/dashboard");

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DbSeeder.SeedAsync(app);
        var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<SlaMonitorJob>(
            "sla-monitor",
            job => job.Execute(CancellationToken.None),
            "*/5 * * * *");
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
