using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace OrderService.Api.Infrastructure.RabbitMq;

public sealed class OrderPlacedPublisher : IAsyncDisposable
{
    private const string ExchangeName = "order.placed.fanout";

    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public OrderPlacedPublisher(IConfiguration configuration)
{
    var factory = new ConnectionFactory
    {
        HostName = configuration["RabbitMq:Host"] ?? "localhost",
        UserName = configuration["RabbitMq:User"] ?? "guest",
        Password = configuration["RabbitMq:Password"] ?? "guest"
    };

    _connection = factory.CreateConnectionAsync()
                         .GetAwaiter()
                         .GetResult();

    _channel = _connection.CreateChannelAsync()
                          .GetAwaiter()
                          .GetResult();

    _channel.ExchangeDeclareAsync(
        exchange: ExchangeName,
        type: ExchangeType.Fanout,
        durable: true)
        .GetAwaiter()
        .GetResult();
}

    public async Task PublishAsync(Guid orderId, Guid customerId, decimal totalAmount)
    {
        var payload = new
        {
            OrderId = orderId,
            CustomerId = customerId,
            TotalAmount = totalAmount,
            OccurredAtUtc = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        var props = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();

        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}