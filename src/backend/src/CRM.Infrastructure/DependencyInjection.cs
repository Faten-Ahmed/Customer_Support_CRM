using CRM.Application.Admin.Users.Commands;
using CRM.Application.Common;
using CRM.Application.Sla.Jobs;
using CRM.Domain.Auth;
using CRM.Domain.Branches;
using CRM.Domain.Categories;
using CRM.Domain.Channels;
using CRM.Domain.Customers;
using CRM.Domain.Departments;
using CRM.Domain.Sla;
using CRM.Domain.Templates;
using CRM.Domain.Tickets;
using CRM.Domain.Users;
using CRM.Infrastructure.Channels;
using CRM.Infrastructure.Email;
using CRM.Infrastructure.Identity;
using CRM.Infrastructure.Jobs;
using CRM.Infrastructure.Notifications;
using CRM.Infrastructure.Persistence;
using CRM.Infrastructure.Persistence.Repositories;
using CRM.Infrastructure.Persistence.Repositories.Sla;
using CRM.Infrastructure.Storage;
using Minio;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core / SQL Server
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Redis — AbortOnConnectFail=false so the app starts even when Redis is not yet running
        var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(redisConnection);
            opts.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(opts);
        });
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection + ",abortConnect=false";
        });

        // Hangfire
        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));
        services.AddHangfireServer();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ICustomerCredentialRepository, CustomerCredentialRepository>();
        services.AddScoped<ICustomerContactRepository, CustomerContactRepository>();
        services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
        services.AddScoped<ITicketMessageRepository, TicketMessageRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ITicketJobScheduler, TicketJobScheduler>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITicketFieldDefinitionRepository, TicketFieldDefinitionRepository>();
        services.AddScoped<IQuickReplyTemplateRepository, QuickReplyTemplateRepository>();
        services.AddScoped<IEmailHealthChecker, EmailHealthChecker>();
        services.AddScoped<ITwilioHealthChecker, TwilioHealthChecker>();
        services.AddScoped<ILiveChatSessionRepository, LiveChatSessionRepository>();
        services.AddScoped<ICategoryExistenceChecker, CategoryExistenceChecker>();

        // SLA repositories
        services.AddScoped<ISlaPolicyRepository, SlaPolicyRepository>();
        services.AddScoped<IBusinessHoursRepository, BusinessHoursRepository>();
        services.AddScoped<ITicketSlaRepository, TicketSlaRepository>();

        // SLA jobs
        services.AddScoped<SlaMonitorJob>();

        // Notification service (stub until Feature 05)
        services.AddScoped<INotificationService, StubNotificationService>();

        services.Configure<MinIOSettings>(configuration.GetSection("MinIO"));
        services.AddScoped<IStorageService, StorageService>();

        // Email service
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailService, EmailService>();

        // JWT token service
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddScoped<ITokenService, TokenService>();

        // Password hasher
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        // Background job service abstraction
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        return services;
    }
}
