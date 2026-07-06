using MediatR;
using OrderService.Api.Infrastructure;
using OrderService.Api.Infrastructure.RabbitMq;
using OrderService.Domain;

namespace OrderService.Api.Features.PlaceOrder;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly OrderDbContext _db;
    private readonly OrderPlacedPublisher _publisher;

    public PlaceOrderHandler(OrderDbContext db, OrderPlacedPublisher publisher){_db = db;
        _publisher = publisher;
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.ProductId, request.Quantity, request.UnitPrice);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        await _publisher.PublishAsync(order.Id, order.CustomerId, order.TotalAmount);

        return new PlaceOrderResult(order.Id, order.Status.ToString());
    }
}