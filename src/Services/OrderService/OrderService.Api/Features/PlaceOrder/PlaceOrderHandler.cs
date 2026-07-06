using MediatR;
using OrderService.Api.Infrastructure;
using OrderService.Domain;

namespace OrderService.Api.Features.PlaceOrder;

public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly OrderDbContext _db;

    public PlaceOrderHandler(OrderDbContext db) => _db = db;

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.ProductId, request.Quantity, request.UnitPrice);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        return new PlaceOrderResult(order.Id, order.Status.ToString());
    }
}