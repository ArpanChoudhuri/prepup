using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Products.AnyAsync(ct))
        {
            db.Products.AddRange(
                new Product { Name = "Explorer Backpack", Price = 79.99m },
                new Product { Name = "Travel Adapter", Price = 19.99m },
                new Product { Name = "First Aid Kit", Price = 29.99m }
            );
        }

        if (!await db.Orders.AnyAsync(ct))
        {
            db.Orders.AddRange(
                new Order { Tenant = "SOS1WEB", ProductId = 1 },
                new Order { Tenant = "SOS1WEB", ProductId = 3 }
            );
        }

        await db.SaveChangesAsync(ct);
    }
}
