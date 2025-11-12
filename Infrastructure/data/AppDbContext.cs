using Infra.models;
using Microsoft.EntityFrameworkCore;

namespace Infra.Data;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItem => Set<OrderItem>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    public DbSet<ProcessedMessage> Processed => Set<ProcessedMessage>();

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

            // configure the collection navigation to OrderItem
            e.HasMany(o => o.Items)
             .WithOne()
             .HasForeignKey(i => i.OrderId)
             .HasPrincipalKey(o => o.Id);
        });

        b.Entity<OrderItem>(e =>
        {
            // make this a proper entity with a composite key
            e.HasKey(x=> new { x.Id });
            e.HasOne(o => o.Product)
             .WithMany()
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<OutboxMessage>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DispatchedUtc, x.NextAttemptUtc });
            e.Property(x => x.Type).HasMaxLength(100);
        });

        b.Entity<ProcessedMessage>(e =>
        {
            e.HasKey(x => x.MessageId);
            e.Property(x => x.MessageId).ValueGeneratedNever();   
        });

    }
}

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public int UnitPrice { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public required string CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public required string Status { get; set; }
    public decimal Total { get; set; }
    public required string Tenant { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public byte[]? RowVersion { get; set; }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}