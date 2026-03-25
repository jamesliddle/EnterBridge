using Microsoft.EntityFrameworkCore;
using EnterBridge.Models;

namespace EnterBridge.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.SubmittedBy).IsRequired().HasMaxLength(100);
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            e.Property(i => i.ProductSku).HasMaxLength(50);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        });
    }
}
