namespace Shared.Contracts.Events;

public record PaymentRefunded
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}