using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger? logger = null)
    {
        var adminExists = await db.Users.AnyAsync(u => u.Role == Role.Admin);
        if (adminExists)
        {
            logger?.LogInformation("[Seeder] Admin user already exists — skipping seed");
            return;
        }

        var admin = User.Create("Admin","admin@example.com",BCrypt.Net.BCrypt.HashPassword("Admin@123"),Role.Admin);

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        logger?.LogInformation("[Seeder] Default admin user seeded (Email: admin@example.com)");
    }
}
