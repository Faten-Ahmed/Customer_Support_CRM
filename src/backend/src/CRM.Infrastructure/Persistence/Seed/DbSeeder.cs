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
