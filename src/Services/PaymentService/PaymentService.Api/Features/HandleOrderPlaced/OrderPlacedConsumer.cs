using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Infrastructure;
using PaymentService.Domain;
using Shared.Contracts.Events;

namespace PaymentService.Api.Features.HandleOrderPlaced;

public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly PaymentDbContext _db;
    private readonly PaymentGatewayClient _gatewayClient;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(PaymentDbContext db, PaymentGatewayClient gatewayClient, ILogger<OrderPlacedConsumer> logger)
    {
        _db = db;
        _gatewayClient = gatewayClient;
        _logger = logger;
    }


    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var messageId = context.MessageId ?? throw new InvalidOperationException("Message has no MessageId.");
        var message = context.Message;

        // Fast path: already-committed duplicate check, no transaction needed yet.
        var alreadyProcessed = await _db.ProcessedMessages.AnyAsync(p => p.MessageId == messageId, context.CancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Duplicate OrderPlaced message {MessageId} for OrderId={OrderId} — skipping.", messageId, message.OrderId);
            return; // Acks the message without redoing any work.
        }

        

        await using var transaction = await _db.Database.BeginTransactionAsync(context.CancellationToken);

        try
        {
            var payment = Payment.CreatePending(message.OrderId, message.TotalAmount);
            _db.Payments.Add(payment);


            bool succeeded;
        try
        {
            succeeded = await _gatewayClient.ChargeAsync(message.OrderId, message.TotalAmount, context.CancellationToken);
        }
        catch (Exception ex)
        {
            // Circuit breaker OPEN, or retries exhausted, or bulkhead rejected the call —
            // all surface here as an exception from the resilience pipeline itself.
            _logger.LogError(ex, "Payment gateway call failed after resilience policies exhausted for OrderId={OrderId}", message.OrderId);
            succeeded = false;
        };

            if (succeeded)
                payment.MarkSucceeded();
            else
                payment.MarkFailed("Simulated gateway decline");

            // Record the inbox entry in the SAME transaction as the business write.
            _db.ProcessedMessages.Add(ProcessedMessage.Create(messageId));

             if (succeeded)
            {
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
                await context.Publish(new PaymentFailed
                {
                    OrderId = message.OrderId,
                    Reason = "Simulated gateway decline",
                    OccurredAtUtc = DateTime.UtcNow
                });
                _logger.LogWarning("Payment failed for OrderId={OrderId}", message.OrderId);
            }

            await _db.SaveChangesAsync(context.CancellationToken);
            await transaction.CommitAsync(context.CancellationToken);

           
        }
        catch (DbUpdateException) when (IsUniqueConstraintViolation())
        {
            // Race: two instances of this consumer processed the same message concurrently
            // (can genuinely happen if you scale PaymentService to 2+ replicas). The unique
            // key on ProcessedMessage.MessageId rejects the second writer at the DB level —
            // our own app-level check above is the fast path, this is the safety net.
            await transaction.RollbackAsync(context.CancellationToken);
            _logger.LogInformation("Concurrent duplicate detected for MessageId={MessageId} — safe to ignore.", messageId);
        }
    }

    private static bool IsUniqueConstraintViolation() => true; // simplified for teaching; real check inspects PostgresException.SqlState == "23505"

    private static bool SimulatePaymentGateway() => Random.Shared.Next(1, 101) > 20;
}