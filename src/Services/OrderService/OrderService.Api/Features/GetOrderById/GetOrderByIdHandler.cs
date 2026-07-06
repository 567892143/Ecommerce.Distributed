using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Api.Infrastructure;

namespace OrderService.Api.Features.GetOrderById;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, GetOrderByIdResult?>
{
    private readonly OrderDbContext _db;

    public GetOrderByIdHandler(OrderDbContext db) => _db = db;

    public async Task<GetOrderByIdResult?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order is null) return null;

        return new GetOrderByIdResult(order.Id, order.CustomerId, order.ProductId, order.Quantity, order.UnitPrice, order.TotalAmount, order.Status.ToString(), order.CreatedAtUtc);
    }
}