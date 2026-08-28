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
                CreateUser("admin@azmsquad.com",  "P@ssw0rd!", "System", "Admin",   UserRole.Admin,   requiresPasswordChange: false),
                CreateUser("manager@azmsquad.com", "P@ssw0rd!", "Sara",   "Manager", UserRole.Manager, requiresPasswordChange: false),
                CreateUser("agent@azmsquad.com",   "P@ssw0rd!", "Omar",   "Hassan",  UserRole.Agent,   requiresPasswordChange: false)
            );

            await db.SaveChangesAsync();
        }
    }

    private static User CreateUser(
        string email, string password, string firstName, string lastName,
        UserRole role, bool requiresPasswordChange)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return User.CreateSeeded(email, hash, firstName, lastName, role, requiresPasswordChange);
    }
}
