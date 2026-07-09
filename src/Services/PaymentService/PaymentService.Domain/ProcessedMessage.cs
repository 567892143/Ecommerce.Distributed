namespace PaymentService.Domain;

public class ProcessedMessage
{
    public Guid MessageId { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedMessage() { }

    public static ProcessedMessage Create(Guid messageId) => new()
    {
        MessageId = messageId,
        ProcessedAtUtc = DateTime.UtcNow
    };
}