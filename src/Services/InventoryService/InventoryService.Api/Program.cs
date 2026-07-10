using InventoryService.Api.Features.HandleReserveInventory;
using InventoryService.Api.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("InventoryDb")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ReserveInventoryConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:User"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        // Explicit, named queue — "reserve-inventory" — instead of letting
        // ConfigureEndpoints() derive a name from the consumer class. This matters
        // because the saga (below) needs to know the EXACT queue name to send to —
        // a command needs a predictable address, unlike an event's fanout exchange.
        cfg.ReceiveEndpoint("reserve-inventory", e =>
        {
            e.ConfigureConsumer<ReserveInventoryConsumer>(context);
        });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();