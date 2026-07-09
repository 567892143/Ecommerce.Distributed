using MassTransit;
using Shared.Contracts.Events;

namespace NotificationService.Api.Features.HandlePaymentEvents;

public class PaymentProcessedConsumer : IConsumer<PaymentProcessed>
{
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(ILogger<PaymentProcessedConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<PaymentProcessed> context)
    {
        _logger.LogInformation("Notified customer: payment succeeded for OrderId={OrderId}", context.Message.OrderId);
        return Task.CompletedTask;
    }
}