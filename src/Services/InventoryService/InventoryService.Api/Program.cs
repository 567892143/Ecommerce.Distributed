using InventoryService.Api.Features.HandleReserveInventory;
using InventoryService.Api.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Shared.BuildingBlocks.Observability;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("InventoryService")) // per-service name
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()   // traces incoming HTTP requests
            .AddHttpClientInstrumentation()   // traces outgoing HTTP calls (your Polly-wrapped gateway client!)
            .AddSource("MassTransit")          // traces publish/consume/send as spans
            .AddConsoleExporter()         // print spans to console for now; Phase 15 could add Jaeger/Zipkin
            .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        });
    });

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

        cfg.UseConsumeFilter(typeof(CorrelationLoggingFilter<>), context);

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

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "InventoryService") // change per service: "OrderService", "InventoryService", "NotificationService"
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();