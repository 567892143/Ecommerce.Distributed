using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentService.Api.Features.HandleOrderPlaced;
using PaymentService.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:User"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ConfigureEndpoints(context);
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