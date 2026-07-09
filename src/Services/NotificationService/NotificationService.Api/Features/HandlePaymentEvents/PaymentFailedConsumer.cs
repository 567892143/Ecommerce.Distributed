using MassTransit;
using Shared.Contracts.Events;

namespace NotificationService.Api.Features.HandlePaymentEvents;

public class PaymentFailedConsumer : IConsumer<PaymentFailed>
{
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(ILogger<PaymentFailedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<PaymentFailed> context)
    {
        var message = context.Message;

  
        // Simulated flaky downstream dependency (e.g., an SMS/email gateway that's
        // intermittently down) — fails ~50% of the time so retry/DLQ behavior is
        // actually observable, not theoretical.
        if (Random.Shared.Next(1, 101) <= 50)
        {
            _logger.LogWarning("Notification gateway unreachable, simulated failure for OrderId={OrderId}", message.OrderId);
            throw new InvalidOperationException("Simulated notification gateway failure");
        }

        _logger.LogInformation("Notified customer: payment failed for OrderId={OrderId}, Reason={Reason}", message.OrderId, message.Reason);
        return Task.CompletedTask;
    }
}