using CRM.Domain.KnowledgeBase;
using CRM.Domain.Sla;
using CRM.Domain.Tickets.Enums;
using CRM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CRM.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                CreateUser("admin@azmsquad.com",   "P@ssw0rd!", "System", "النظام",  "Admin",   "مدير",   UserRole.Admin,   requiresPasswordChange: false),
                CreateUser("manager@azmsquad.com", "P@ssw0rd!", "Sara",   "سارة",    "Manager", "مديرة",  UserRole.Manager, requiresPasswordChange: false),
                CreateUser("agent@azmsquad.com",   "P@ssw0rd!", "Omar",   "عمر",     "Hassan",  "حسن",    UserRole.Agent,   requiresPasswordChange: false)
            );

            await db.SaveChangesAsync();
        }

        if (!await db.KbCategories.AnyAsync())
        {
            db.KbCategories.AddRange(
                KbCategory.Create("General"),
                KbCategory.Create("Account & Billing"),
                KbCategory.Create("Technical Support"),
                KbCategory.Create("Getting Started"),
                KbCategory.Create("Policies & Compliance")
            );
            await db.SaveChangesAsync();
        }

        if (!await db.SlaPolicies.AnyAsync())
        {
            db.SlaPolicies.AddRange(
                SlaPolicy.Create(TicketPriority.Critical, firstResponseMinutes: 15,  resolutionMinutes: 60,  warningThresholdPercent: 75, breachThresholdPercent: 100, criticalBreachThresholdPercent: 150),
                SlaPolicy.Create(TicketPriority.High,     firstResponseMinutes: 60,  resolutionMinutes: 240, warningThresholdPercent: 75, breachThresholdPercent: 100, criticalBreachThresholdPercent: 150),
                SlaPolicy.Create(TicketPriority.Medium,   firstResponseMinutes: 240, resolutionMinutes: 1440, warningThresholdPercent: 75, breachThresholdPercent: 100, criticalBreachThresholdPercent: 150),
                SlaPolicy.Create(TicketPriority.Low,      firstResponseMinutes: 480, resolutionMinutes: 2880, warningThresholdPercent: 75, breachThresholdPercent: 100, criticalBreachThresholdPercent: 150)
            );
            await db.SaveChangesAsync();
        }

        if (!await db.BusinessHours.AnyAsync())
        {
            db.BusinessHours.Add(
                BusinessHours.Create(
                    workDays: ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday"],
                    startTime: new TimeOnly(9, 0),
                    endTime: new TimeOnly(17, 0),
                    timeZone: "Asia/Riyadh")
            );
            await db.SaveChangesAsync();
        }
    }

    private static User CreateUser(
        string email, string password,
        string firstName, string firstNameAr,
        string lastName, string lastNameAr,
        UserRole role, bool requiresPasswordChange)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return User.CreateSeeded(email, hash, firstName, firstNameAr, lastName, lastNameAr, role, requiresPasswordChange);
    }
}
