using Microsoft.EntityFrameworkCore;

namespace Infra.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // In normal runs apply migrations. In tests (SQLite in-memory) migrations may be absent
        // and EF will report pending model changes. Fall back to EnsureCreatedAsync for those cases.
        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch (InvalidOperationException)
        {
            // If the provider is Sqlite (common for tests) create the schema from the model instead.
            // Re-throw for other providers to avoid hiding real problems.
            if (db.Database.ProviderName?.IndexOf("Sqlite", StringComparison.OrdinalIgnoreCase) == 0
                || db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
            {
                await db.Database.EnsureCreatedAsync(ct);
            }
            else
            {
                throw;
            }
        }

        if (!await db.Products.AnyAsync(ct))
        {
            db.Products.AddRange(
                new Product { Name = "Explorer Backpack", Price = 79.99m, IsActive=true, UnitPrice= 100 },
                new Product { Name = "Travel Adapter", Price = 19.99m, IsActive = true, UnitPrice = 100 },
                new Product { Name = "First Aid Kit", Price = 29.99m, IsActive = true, UnitPrice = 100 }
            );
        }

        if (!await db.Orders.AnyAsync(ct))
        {
            db.Orders.AddRange(
                new Order {CustomerId="1",Status="active", Tenant = "TEN1ANT1", Total=200},
                new Order {CustomerId = "1", Status="active", Tenant = "TEN1ANT1", Total=200 }
            );
        }

        if (!await db.OrderItem.AnyAsync(ct))
        {
            db.OrderItem.AddRange(
                new OrderItem { Id=Guid.NewGuid(), OrderId= db.Orders.ToList().FirstOrDefault().Id, Product=db.Products.ToList().FirstOrDefault(), LineTotal=100, Quantity=2, UnitPrice=100 }
            );
        }

        await db.SaveChangesAsync(ct);
    }
}
