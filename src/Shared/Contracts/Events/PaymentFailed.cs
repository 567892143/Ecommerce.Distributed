namespace Shared.Contracts.Events;

public record PaymentFailed
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = default!;
    public DateTime OccurredAtUtc { get; init; }
}