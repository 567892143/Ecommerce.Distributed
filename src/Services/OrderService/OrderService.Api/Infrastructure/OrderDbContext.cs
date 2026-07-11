using MassTransit;
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

        modelBuilder.Entity<OrderService.Api.Sagas.OrderSagaState>(builder =>
{
    builder.ToTable("order_saga_state");
    builder.HasKey(x => x.CorrelationId);
    builder.Property(x => x.CurrentState).HasMaxLength(64);
    builder.Property(x => x.ProductId).IsRequired();
    builder.Property(x => x.Quantity).IsRequired();
    builder.Property(x => x.SubmittedAtUtc).IsRequired();
    builder.Property(x => x.PaymentId);
    builder.Property(x => x.AmountCharged).HasColumnType("decimal(18,2)");
});

   

       modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}