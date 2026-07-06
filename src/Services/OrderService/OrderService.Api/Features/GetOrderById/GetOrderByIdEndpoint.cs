using MediatR;

namespace OrderService.Api.Features.GetOrderById;

public static class GetOrderByIdEndpoint
{
    public static void MapGetOrderByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{orderId:guid}", async (Guid orderId, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderByIdQuery(orderId));
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetOrderById")
        .Produces<GetOrderByIdResult>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}