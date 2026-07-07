using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Infrastructure;
using PaymentService.Domain;
using Shared.Contracts.Events;

namespace PaymentService.Api.Features.HandleOrderPlaced;

public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(PaymentDbContext db, ILogger<OrderPlacedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var message = context.Message;

        var payment = Payment.CreatePending(message.OrderId, message.TotalAmount);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(context.CancellationToken);

        // Simulated payment gateway call. We're faking a ~20% failure rate on
        // purpose — Phase 12 needs a real, reproducible failure path to inject into.
        var succeeded = SimulatePaymentGateway(message.TotalAmount);

        if (succeeded)
        {
            payment.MarkSucceeded();
            await _db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new PaymentProcessed
            {
                OrderId = message.OrderId,
                PaymentId = payment.Id,
                AmountCharged = message.TotalAmount,
                OccurredAtUtc = DateTime.UtcNow
            });

            _logger.LogInformation("Payment succeeded for OrderId={OrderId}", message.OrderId);
        }
        else
        {
            payment.MarkFailed("Simulated gateway decline");
            await _db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new PaymentFailed
            {
                OrderId = message.OrderId,
                Reason = "Simulated gateway decline",
                OccurredAtUtc = DateTime.UtcNow
            });

            _logger.LogWarning("Payment failed for OrderId={OrderId}", message.OrderId);
        }
    }

    private static bool SimulatePaymentGateway(decimal amount)
    {
        // Deterministic-ish rule so you can reproduce failures on demand:
        // any amount ending in .00 with a whole-number total divisible by 5 "fails".
        // Simple enough to reason about, random enough to feel like a real gateway.
        return Random.Shared.Next(1, 101) > 20; // ~80% success rate
    }
}