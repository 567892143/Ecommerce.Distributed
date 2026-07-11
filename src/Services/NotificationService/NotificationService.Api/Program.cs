using MassTransit;
using NotificationService.Api.Features.HandlePaymentEvents;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("NotificationService")) // per-service name
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

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentFailedConsumer>(cfg =>
    {
        // Per-consumer retry policy: 3 attempts, with increasing delay between each.
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10)));
    });

    x.AddConsumer<PaymentProcessedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:User"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.UseConsumeFilter(typeof(CorrelationLoggingFilter<>), context);


        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "NotificationService") // change per service: "OrderService", "InventoryService", "NotificationService"
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