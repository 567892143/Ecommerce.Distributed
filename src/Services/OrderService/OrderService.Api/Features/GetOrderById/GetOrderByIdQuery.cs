using MediatR;

namespace OrderService.Api.Features.GetOrderById;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<GetOrderByIdResult?>;

public record GetOrderByIdResult(Guid OrderId, Guid CustomerId, Guid ProductId, int Quantity, decimal UnitPrice, decimal TotalAmount, string Status, DateTime CreatedAtUtc);