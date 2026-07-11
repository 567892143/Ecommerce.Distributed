using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PaymentService.Api.Features.HandleOrderPlaced;
using PaymentService.Api.Features.HandleRefundPayment;
using PaymentService.Api.Infrastructure;
using Polly;
using Polly.Timeout;
using Serilog;
using Shared.BuildingBlocks.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("PaymentService")) // per-service name
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()   // traces incoming HTTP requests
            .AddHttpClientInstrumentation()   // traces outgoing HTTP calls (your Polly-wrapped gateway client!)
            .AddSource("MassTransit")          // traces publish/consume/send as spans
            .AddConsoleExporter()             // print spans to console for now; Phase 15 could add Jaeger/Zipkin
                .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        });
    });

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<RefundPaymentConsumer>();
      x.AddEntityFrameworkOutbox<PaymentDbContext>(o =>
    {
        o.UsePostgres();               // tells MassTransit which SQL dialect to generate for the outbox tables
        o.UseBusOutbox();              // <-- this is the switch that reroutes ALL publishes through the outbox
        o.QueryDelay = TimeSpan.FromSeconds(1); // how often the outbox delivery service polls for unsent messages
    });
    x.AddConsumer<OrderPlacedConsumer>();

    

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

builder.Services.AddHttpClient<PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PaymentGateway:BaseUrl"] ?? "http://localhost:5099");
})
.AddResilienceHandler("payment-gateway-pipeline", pipelineBuilder =>
{
     pipelineBuilder.AddConcurrencyLimiter(permitLimit: 10, queueLimit: 5);
    // 1. TIMEOUT — innermost, wraps the actual call. If the gateway hangs (our
    //    simulated 3-6s delay), don't wait forever; give up after 2 seconds and
    //    treat it as a failure the outer policies can react to.
    pipelineBuilder.AddTimeout(TimeSpan.FromSeconds(2));

    // 2. RETRY — wraps the timeout. If a call times out OR the gateway returns
    //    a transient failure, retry a few times with exponential backoff before
    //    giving up entirely.
    pipelineBuilder.AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromMilliseconds(500),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(r => (int)r.StatusCode >= 500)
    });

    // 3. CIRCUIT BREAKER — outermost of the "reactive" policies. If failures
    //    keep happening even after retries are exhausted, STOP calling the
    //    gateway entirely for a cooldown period. This protects the gateway
    //    from being hammered by a flood of retries while it's already down,
    //    and protects PaymentService from wasting time/threads on calls
    //    that are very likely to fail anyway.
    pipelineBuilder.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 4,
        BreakDuration = TimeSpan.FromSeconds(15),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(r => (int)r.StatusCode >= 500)
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "PaymentService") // change per service: "OrderService", "InventoryService", "NotificationService"
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