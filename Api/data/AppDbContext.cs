using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.HasIndex(x => x.Name);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.Property(x => x.RowVersion).IsRowVersion(); // concurrency token
        });

        b.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            //e.HasIndex(x => new { x.Tenant, x.CreatedUtc });
            e.Property(x => x.RowVersion).IsRowVersion(); // concurrency token
        });
    }
}

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public required string Tenant { get; set; }
    public int ProductId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}
