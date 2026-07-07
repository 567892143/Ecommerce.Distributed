using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Api.Infrastructure;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(builder =>
        {
            builder.ToTable("payments");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.OrderId).IsRequired();
            builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(p => p.Status).HasConversion<string>().IsRequired();
            builder.Property(p => p.FailureReason).HasMaxLength(500);
            builder.Property(p => p.CreatedAtUtc).IsRequired();
        });
    }
}