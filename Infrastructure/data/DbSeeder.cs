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
                new Product { Name = "Explorer Backpack", Price = 79.99m, IsActive = true, UnitPrice = 100 },
                new Product { Name = "Travel Adapter", Price = 19.99m, IsActive = true, UnitPrice = 100 },
                new Product { Name = "First Aid Kit", Price = 29.99m, IsActive = true, UnitPrice = 100 }
            );
        }

        if (!await db.Orders.AnyAsync(ct))
        {
            db.Orders.AddRange(
                new Order { CustomerId = "1", Status = "active", Tenant = "TEN1ANT1", Total = 200 },
                new Order { CustomerId = "1", Status = "active", Tenant = "TEN1ANT1", Total = 200 }
            );
        }

        // If we added products or orders above they are only tracked locally until SaveChanges is called.
        // Persist them now so subsequent queries (used to create order items) will return the expected entities.
        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
        }

        if (!await db.OrderItem.AnyAsync(ct))
        {
            // Query the persisted data (guaranteed to exist after the SaveChanges above when needed)
            var firstOrder = await db.Orders.OrderBy(o => o.Id).FirstOrDefaultAsync(ct);
            var firstProduct = await db.Products.OrderBy(p => p.Id).FirstOrDefaultAsync(ct);

            // Defensive null checks to avoid NullReferenceException in case something unexpected happened.
            if (firstOrder != null && firstProduct != null)
            {
                db.OrderItem.AddRange(
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = firstOrder.Id,
                        Product = firstProduct,
                        LineTotal = 100,
                        Quantity = 2,
                        UnitPrice = 100
                    }
                );

                await db.SaveChangesAsync(ct);
            }
        }
    }
}
