using MediatR;

namespace OrderService.Api.Features.PlaceOrder;

public record PlaceOrderCommand(Guid CustomerId, Guid ProductId, int Quantity, decimal UnitPrice)
    : IRequest<PlaceOrderResult>;

public record PlaceOrderResult(Guid OrderId, string Status);