namespace Shared.Contracts.Events;

public record PaymentProcessed
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal AmountCharged { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}