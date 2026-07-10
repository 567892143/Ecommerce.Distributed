using Microsoft.EntityFrameworkCore;
using InventoryService.Domain;

namespace InventoryService.Api.Infrastructure;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductStock>(builder =>
        {
            builder.ToTable("product_stocks");
            builder.HasKey(p => p.ProductId);
            builder.Property(p => p.AvailableQuantity).IsRequired();
            builder.Property(p => p.ReservedQuantity).IsRequired();
        });
    }
}