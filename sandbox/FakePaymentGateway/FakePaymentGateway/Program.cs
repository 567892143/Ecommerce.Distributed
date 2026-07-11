var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/charge", async (ChargeRequest request) =>
{
    // Simulate real-world unreliability: ~15% hard failure, ~15% slow (3-6s), rest fast+success
    var roll = Random.Shared.Next(1, 101);

    if (roll <= 15)
    {
        return Results.StatusCode(503); 
    }

    if (roll <= 30)
    {
        await Task.Delay(Random.Shared.Next(3000, 6000)); 
    }

    return Results.Ok(new { Approved = true, TransactionId = Guid.NewGuid() });
});

app.Run();

record ChargeRequest(Guid OrderId, decimal Amount);