using MediatR;

namespace OrderService.Api.Features.PlaceOrder;

public static class PlaceOrderEndpoint
{
    public static void MapPlaceOrderEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (PlaceOrderCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Created($"/orders/{result.OrderId}", result);
        })
        .WithName("PlaceOrder")
        .Produces<PlaceOrderResult>(StatusCodes.Status201Created)
        .ProducesValidationProblem();
    }
}