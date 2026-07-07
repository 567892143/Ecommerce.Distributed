using MassTransit;
using Shared.Contracts.Events;

namespace OrderService.Api.Features.Diagnostics;

public class OrderPlacedLoggingConsumer : IConsumer<OrderPlaced>
{
    private readonly ILogger<OrderPlacedLoggingConsumer> _logger;

    public OrderPlacedLoggingConsumer(ILogger<OrderPlacedLoggingConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        _logger.LogInformation(
            "OrderPlaced consumed: OrderId={OrderId}, CustomerId={CustomerId}, Total={Total}",
            context.Message.OrderId, context.Message.CustomerId, context.Message.TotalAmount);

        return Task.CompletedTask;
    }
}