using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

const string ExchangeName = "order.placed.fanout";
const string QueueName = "sandbox.order.placed.queue";

var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "guest",
    Password = "guest"
};

await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(
    exchange: ExchangeName,
    type: ExchangeType.Fanout,
    durable: true);

await channel.QueueDeclareAsync(
    queue: QueueName,
    durable: true,
    exclusive: false,
    autoDelete: false);

await channel.QueueBindAsync(
    queue: QueueName,
    exchange: ExchangeName,
    routingKey: string.Empty);

// Only one unacked message delivered at a time.
await channel.BasicQosAsync(
    prefetchSize: 0,
    prefetchCount: 1,
    global: false);

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (_, ea) =>
{
    var body = Encoding.UTF8.GetString(ea.Body.ToArray());

    Console.WriteLine($"[Received] {body}");

    // Simulate processing if needed
    // await Task.Delay(1000);

    // Manual acknowledgement
    await channel.BasicAckAsync(
        deliveryTag: ea.DeliveryTag,
        multiple: false);
};

await channel.BasicConsumeAsync(
    queue: QueueName,
    autoAck: false,
    consumer: consumer);

Console.WriteLine("Listening for OrderPlaced messages. Press Enter to exit.");
Console.ReadLine();