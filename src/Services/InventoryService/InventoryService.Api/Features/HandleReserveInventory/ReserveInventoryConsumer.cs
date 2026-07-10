using MassTransit;
using Microsoft.EntityFrameworkCore;
using InventoryService.Api.Infrastructure;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace InventoryService.Api.Features.HandleReserveInventory;

public class ReserveInventoryConsumer : IConsumer<ReserveInventory>
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<ReserveInventoryConsumer> _logger;

    public ReserveInventoryConsumer(InventoryDbContext db, ILogger<ReserveInventoryConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveInventory> context)
    {
        var message = context.Message;

        var stock = await _db.ProductStocks.FirstOrDefaultAsync(p => p.ProductId == message.ProductId, context.CancellationToken);

        if (stock is null)
        {
            await context.Publish(new InventoryFailed
            {
                OrderId = message.OrderId,
                ProductId = message.ProductId,
                Reason = "Product not found in inventory",
                OccurredAtUtc = DateTime.UtcNow
            });
            _logger.LogWarning("Inventory reservation failed — unknown product {ProductId} for OrderId={OrderId}", message.ProductId, message.OrderId);
            return;
        }

        // NOTE: apply the same Inbox idempotency pattern here that you built by hand
        // in Phase 6, for the same reason — this consumer is subject to at-least-once
        // delivery exactly like PaymentService's was. Left as a deliberate exercise:
        // wire up a ProcessedMessage-style table for InventoryService before Phase 12,
        // where we'll deliberately test duplicate ReserveInventory commands.

        if (stock.TryReserve(message.Quantity))
        {
            await _db.SaveChangesAsync(context.CancellationToken);

            await context.Publish(new InventoryReserved
            {
                OrderId = message.OrderId,
                ProductId = message.ProductId,
                Quantity = message.Quantity,
                OccurredAtUtc = DateTime.UtcNow
            });
            _logger.LogInformation("Inventory reserved for OrderId={OrderId}", message.OrderId);
        }
        else
        {
            await context.Publish(new InventoryFailed
            {
                OrderId = message.OrderId,
                ProductId = message.ProductId,
                Reason = "Insufficient stock",
                OccurredAtUtc = DateTime.UtcNow
            });
            _logger.LogWarning("Inventory reservation failed — insufficient stock for OrderId={OrderId}", message.OrderId);
        }
    }
}