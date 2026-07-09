using MassTransit;
using MediatR;
using OrderService.Api.Infrastructure;
using OrderService.Api.Infrastructure.RabbitMq;
using OrderService.Domain;
using Shared.Contracts.Events;

namespace OrderService.Api.Features.PlaceOrder;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly OrderDbContext _db;
    //private readonly OrderPlacedPublisher _publisher;

    private readonly IPublishEndpoint _publishEndpoint;

    public PlaceOrderHandler(OrderDbContext db,IPublishEndpoint publishEndpoint){_db = db;
      //  _publisher = publisher;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.ProductId, request.Quantity, request.UnitPrice);

        _db.Orders.Add(order);
       

       // await _publisher.PublishAsync(order.Id, order.CustomerId, order.TotalAmount);

           await _publishEndpoint.Publish(new OrderPlaced
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            ProductId = order.ProductId,
            Quantity = order.Quantity,
            TotalAmount = order.TotalAmount,
            OccurredAtUtc = DateTime.UtcNow
        },context => {context.CorrelationId = order.Id ;}, ct);

         await _db.SaveChangesAsync(ct);
     
        return new PlaceOrderResult(order.Id, order.Status.ToString());
    }
}