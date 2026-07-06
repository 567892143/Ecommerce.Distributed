using Microsoft.EntityFrameworkCore;
using OrderService.Domain;

namespace OrderService.Api.Infrastructure;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("orders");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o => o.ProductId).IsRequired();
            builder.Property(o => o.Quantity).IsRequired();
            builder.Property(o => o.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.Status).HasConversion<string>().IsRequired();
            builder.Property(o => o.CreatedAtUtc).IsRequired();
            builder.Ignore(o => o.TotalAmount); // computed, not persisted
        });
    }
}