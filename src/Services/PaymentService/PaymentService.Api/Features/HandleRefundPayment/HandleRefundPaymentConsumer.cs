using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Infrastructure;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace PaymentService.Api.Features.HandleRefundPayment;

public class RefundPaymentConsumer : IConsumer<RefundPayment>
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<RefundPaymentConsumer> _logger;

    public RefundPaymentConsumer(PaymentDbContext db, ILogger<RefundPaymentConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundPayment> context)
    {
        var message = context.Message;

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == message.PaymentId, context.CancellationToken);

        if (payment is null)
        {
            _logger.LogError("RefundPayment received for unknown PaymentId={PaymentId}, OrderId={OrderId}", message.PaymentId, message.OrderId);
            return; // Nothing to compensate — this itself would be worth alerting on in a real system.
        }

        if (payment.Status == PaymentService.Domain.PaymentStatus.Refunded)
        {
            // Idempotency guard: a redelivered RefundPayment command should not throw
            // via MarkRefunded's guard clause — it should just be a safe no-op.
            _logger.LogInformation("PaymentId={PaymentId} already refunded — skipping duplicate refund command.", message.PaymentId);
            return;
        }

        payment.MarkRefunded();
        await _db.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new PaymentRefunded
        {
            OrderId = message.OrderId,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            OccurredAtUtc = DateTime.UtcNow
        });

        _logger.LogWarning("Payment REFUNDED for OrderId={OrderId}, PaymentId={PaymentId}, Amount={Amount}", message.OrderId, payment.Id, payment.Amount);
    }
}